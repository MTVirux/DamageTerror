using System.Collections.Concurrent;
using System.Globalization;

namespace DamageTerror.Services;

public sealed partial class SkillTracker
{
    private readonly object syncLock = new();

    private Dictionary<string, Dictionary<string, SkillAccum>> damageData = new();
    private Dictionary<string, Dictionary<string, SkillAccum>> healData = new();
    private Dictionary<string, Dictionary<string, SkillAccum>> dotTickData = new();
    private Dictionary<string, Dictionary<string, SkillAccum>> hotTickData = new();

#if DEBUG
    // Diagnostic counters for DoT processing
    private int dotHotLineCount;
    private int dotLineCount;
    private int dotAggregateCount;
    private int dotGroundEffectCount;
    private int dotStatusFoundCount;
    private int dotFallbackCount;
    private long dotTotalDamageDistributed;

    public string GetDotDiagnostics()
    {
        lock (syncLock)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"T24 total={dotHotLineCount}, DoT={dotLineCount}, agg={dotAggregateCount}, ge={dotGroundEffectCount}");
            sb.AppendLine($"statusFound={dotStatusFoundCount}, fallback={dotFallbackCount}, totalDmg={dotTotalDamageDistributed:N0}");
            foreach (var (src, skills) in damageData)
            {
                var dotOnly = dotTickData.TryGetValue(src, out var dt) ? dt.Sum(kv => kv.Value.Amount) : 0;
                if (dotOnly > 0)
                    sb.AppendLine($"  {src}: dotTickDmg={dotOnly:N0}");
            }
            return sb.ToString();
        }
    }
#endif

    private readonly Dictionary<string, List<SkillUseEvent>> skillEvents = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<SkillUseEvent>> damageTakenEvents = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<SkillUseEvent>> itemEvents = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> stunCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> skillIssueCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(string Target, string Status), int> skillIssueStacks = new();
    private readonly Dictionary<string, int> damageDownCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(string Target, string Status), int> damageDownStacks = new();
    private readonly Dictionary<string, int> positionalHitCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> positionalMissCounts = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Status IDs whose presence on a target causes incoming damage to be reflected
    /// back to the attacker. The reflect arrives as a second 0x03 damage effect inside the
    /// attacker's Type 21/22 line and must be re-attributed to the target.
    /// Sourced from each job definition's <see cref="JobDefinitionBase.KnownReflectStatusIds"/>.</summary>
    private static readonly HashSet<uint> ReflectStatusIds = JobRegistry.GetKnownReflectStatusIds();

    private Dictionary<string, List<SkillUseEvent>>? seededEvents;
    private Dictionary<string, List<SkillUseEvent>>? seededDamageTakenEvents;
    private Dictionary<string, List<SkillUseEvent>>? seededItemEvents;

    private EncounterTimer? timer;
    private GraphDataTracker? graphTracker;
    private StatusTracker? statusTracker;
    private readonly ConcurrentDictionary<uint, SkillDamageType> damageTypeCache = new();
    private readonly Dictionary<string, CombatantDotStats> combatantDotStats = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Pending low-byte refinement data extracted from 0x0E/0x0F status-application
    /// effects in Type 21/22 lines, consumed when the matching Type 26 (GainsEffect) arrives.</summary>
    private readonly Dictionary<(string Source, string Target, uint StatusId), (byte DamageLowByte, byte CritLowByte)> pendingLowBytes = new();

    /// <summary>Maps entity hex ID → entity name for all combatants seen via Type 03 (AddCombatant).</summary>
    private readonly Dictionary<string, string> entityIdToName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Maps pet entity hex ID → owner entity hex ID for pet-to-owner remapping.</summary>
    private readonly Dictionary<string, string> petToOwnerId = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Known ground-effect entity names sourced from GroundEffectDots values across all job definitions
    /// (e.g. "Earthly Star", "Salted Earth"). Used to detect when a ground entity is the source of a Type 21/22 line.</summary>
    private static readonly HashSet<string> GroundEffectEntityNames = new(
        JobRegistry.GetGroundEffectDotIds().Values.Distinct(), StringComparer.OrdinalIgnoreCase);

    /// <summary>Maps ground-effect entity name → owner (player) name.
    /// Populated when a player uses a ground-effect placement skill (Type 21/22).</summary>
    private readonly Dictionary<string, string> groundEffectEntityOwners = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Pet skill accumulation: owner → pet name → skill name → accum.
    /// Displayed as category entries in the skill breakdown.</summary>
    private readonly Dictionary<string, Dictionary<string, Dictionary<string, SkillAccum>>> petDamageData = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, Dictionary<string, SkillAccum>>> petHealData = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Default crit damage multiplier fallback when per-source dynamic estimate is unavailable.</summary>
    private const double DefaultCritMulti = 1.65;

    private const byte CritFlag = 0x20;
    private const byte DirectHitFlag = 0x40;

    private const double DotOutlierLowThreshold = 0.3;
    private const double DotOutlierHighThreshold = 3.0;
    private const int DotMinHitsForOutlierFilter = 10;

    /// <summary>FFXIV player entity IDs start with 0x10; enemies start with 0x40.</summary>
    private static bool IsPlayerEntity(string hexEntityId)
        => hexEntityId.Length >= 2 && hexEntityId[0] == '1' && hexEntityId[1] == '0';

    private readonly IDataManager dataManager;
    private readonly IPluginLog log;
    private readonly PositionalTable positionalTable;
    private readonly Configuration config;

    public SkillTracker(IDataManager dataManager, IPluginLog log, PositionalTable positionalTable, Configuration config)
    {
        this.dataManager = dataManager;
        this.log = log;
        this.positionalTable = positionalTable;
        this.config = config;
    }

    /// <summary>Bind the shared encounter timer, graph tracker, and status tracker.</summary>
    public void SetDependencies(EncounterTimer encounterTimer, GraphDataTracker tracker, StatusTracker statusTracker)
    {
        timer = encounterTimer;
        graphTracker = tracker;
        this.statusTracker = statusTracker;
    }

    /// <summary>
    /// Pre-load historical skill events so that <see cref="GetSkillEvents"/> returns
    /// them until live data is available. Cleared on the next <see cref="Reset"/> call.
    /// </summary>
    public void SeedHistoricalEvents(Dictionary<string, List<SkillUseEvent>> data)
    {
        lock (syncLock)
        {
            seededEvents = new Dictionary<string, List<SkillUseEvent>>(data, StringComparer.OrdinalIgnoreCase);
        }
    }

    public void SeedHistoricalDamageTakenEvents(Dictionary<string, List<SkillUseEvent>> data)
    {
        lock (syncLock)
        {
            seededDamageTakenEvents = new Dictionary<string, List<SkillUseEvent>>(data, StringComparer.OrdinalIgnoreCase);
        }
    }

    public void SeedHistoricalItemEvents(Dictionary<string, List<SkillUseEvent>> data)
    {
        lock (syncLock)
        {
            seededItemEvents = new Dictionary<string, List<SkillUseEvent>>(data, StringComparer.OrdinalIgnoreCase);
        }
    }

    private struct SkillAccum
    {
        public long Amount;
        public int Hits;
        public int Crits;
        public int DirectHits;
        public int CritDirectHits;
        public SkillDamageType DamageType;
    }

    /// <summary>
    /// Running stats for DoT/HoT tick simulation per combatant.
    /// Tracks base damage (crit/DH stripped), crit rate, and DH rate from ability hits.
    /// See: https://github.com/ravahn/FFXIV_ACT_Plugin/wiki/DoT---HoT-Simulation-details
    /// </summary>
    private struct CombatantDotStats
    {
        public double TotalBaseDamage;
        public int TotalHits;
        public int CritHits;
        public int DHHits;

        // DH-stripped damage pools split by crit/non-crit for dynamic crit multiplier estimation.
        public double NonCritDHStripped;
        public int NonCritCount;
        public double CritDHStripped;
        public int CritCountForMulti;

        // Per-potency coefficient calibrated from DoT initial hits.
        public double CalibrationSum;
        public int CalibrationCount;

        public readonly double AverageBaseDmgPerHit => TotalHits > 0 ? TotalBaseDamage / TotalHits : 0;
        public readonly double CritRate => TotalHits > 0 ? (double)CritHits / TotalHits : 0;
        public readonly double DHRate => TotalHits > 0 ? (double)DHHits / TotalHits : 0;
        public readonly bool HasData => TotalHits >= 3;

        /// <summary>
        /// Dynamic crit multiplier derived from observed crit vs non-crit damage ratios.
        /// Returns 0 when insufficient data is available (falls back to DefaultCritMulti).
        /// </summary>
        public readonly double DynamicCritMulti
        {
            get
            {
                if (NonCritCount < 5 || CritCountForMulti < 3) return 0;
                var avgNonCrit = NonCritDHStripped / NonCritCount;
                var avgCrit = CritDHStripped / CritCountForMulti;
                if (avgNonCrit <= 0) return 0;
                var multi = avgCrit / avgNonCrit;
                return multi is >= 1.3 and <= 1.8 ? multi : 0;
            }
        }

        public readonly double CalibratedCoeff => CalibrationCount > 0 ? CalibrationSum / CalibrationCount : 0;
        public readonly bool HasCalibration => CalibrationCount >= 1;
    }

    public void ProcessLogLine(string[] line)
    {
        if (line.Length < 2)
            return;

        var type = line[0];

        if (type == "03")
        {
            ProcessAddCombatant(line);
            if (line.Length >= 7)
                ServiceManager.LogDebug(LogChannel.PetDebug, $"[PetDebug] Type03 id={line[2]} name={line[3]} ownerId={line[6]}");
            return;
        }

        // Debug: log all Type 21/22 lines where source or skill matches a ground-effect entity name.
        if ((type == "21" || type == "22") && line.Length >= 10)
        {
            var dbgSrc = line[3];
            var dbgSkill = line[5];
            if (GroundEffectEntityNames.Contains(dbgSrc) || GroundEffectEntityNames.Contains(dbgSkill))
                ServiceManager.LogDebug(LogChannel.PetDebug, $"[PetDebug] Type{type} srcId={line[2]} src={dbgSrc} actId={line[4]} skill={dbgSkill} tgt={line[7]}");
        }

        if (type == "26" || type == "30")
        {
            ProcessStatusLine(type, line);
            return;
        }

        if (type == "24")
        {
            ProcessDoTHoTLine(line);
            return;
        }

        if (type != "21" && type != "22")
            return;

        ProcessAbilityLine(line);
    }

    private int GetCountLocked(Dictionary<string, int> dict, string combatantName)
    {
        lock (syncLock)
        {
            return dict.GetValueOrDefault(combatantName);
        }
    }

    private void ProcessAddCombatant(string[] line)
    {
        if (line.Length < 7)
            return;

        var entityId = line[2];
        var entityName = line[3];
        var ownerId = line[6];

        if (string.IsNullOrEmpty(entityId) || string.IsNullOrEmpty(entityName))
            return;

        lock (syncLock)
        {
            entityIdToName[entityId] = entityName;

            if (!string.IsNullOrEmpty(ownerId)
                && ownerId != "0"
                && long.TryParse(ownerId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var ownerIdNum)
                && ownerIdNum != 0)
            {
                petToOwnerId[entityId] = ownerId;
            }
        }
    }

    public void Reset()
    {
        lock (syncLock)
        {
            damageData.Clear();
            healData.Clear();
            dotTickData.Clear();
            hotTickData.Clear();
            skillEvents.Clear();
            damageTakenEvents.Clear();
            stunCounts.Clear();
            skillIssueCounts.Clear();
            skillIssueStacks.Clear();
            damageDownCounts.Clear();
            damageDownStacks.Clear();
            positionalHitCounts.Clear();
            positionalMissCounts.Clear();
            itemEvents.Clear();
            seededEvents = null;
            seededDamageTakenEvents = null;
            seededItemEvents = null;
            damageTypeCache.Clear();
            combatantDotStats.Clear();
            pendingLowBytes.Clear();
            groundEffectEntityOwners.Clear();
            petDamageData.Clear();
            petHealData.Clear();
            // entityIdToName, petToOwnerId are zone-level; survive encounter resets.
        }
    }

    private readonly struct AbilityLineContext
    {
        public required string SourceId { get; init; }
        public required string SourceName { get; init; }
        public string? TargetName { get; init; }
        public required string SkillName { get; init; }
        public uint ActionId { get; init; }
        public SkillDamageType DamageType { get; init; }
        public string? PetOwnerName { get; init; }
        public string? PetEntityName { get; init; }
    }

    private readonly struct AbilityEffectAmounts
    {
        public long Damage { get; init; }
        public byte DamageSeverity { get; init; }
        public int DamageBonusPercent { get; init; }
        public long Heal { get; init; }
        public byte HealSeverity { get; init; }
        public long Reflect { get; init; }
        public byte ReflectSeverity { get; init; }

        public bool HasDamageOrHeal => Damage > 0 || Heal > 0;
    }

}
