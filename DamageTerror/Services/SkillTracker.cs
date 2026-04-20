using System.Collections.Concurrent;
using System.Globalization;
using Dalamud.Plugin.Services;

namespace DamageTerror.Services;

public sealed class SkillTracker
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

    /// <summary>Localized status names that count as "skill issues" (Vulnerability Up / Damage Down).</summary>
    private static readonly HashSet<string> SkillIssueNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // EN
        "Vulnerability Up",
        "Damage Down",
        // DE
        "Erhöhte Verwundbarkeit",
        "Schaden -",
        // FR
        "Vulnérabilité augmentée",
        "Malus de dégâts",
        // JA
        "被ダメージ上昇",
        "ダメージ低下",
    };

    /// <summary>Localized status names that count as "Damage Down" only (subset of skill issues).</summary>
    private static readonly HashSet<string> DamageDownNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // EN
        "Damage Down",
        // DE
        "Schaden -",
        // FR
        "Malus de dégâts",
        // JA
        "ダメージ低下",
    };

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

        if (line.Length < 10)
            return;

        if (type != "21" && type != "22")
            return;

        var sourceName = line[3];
        var targetName = line.Length > 7 ? line[7] : null;
        var skillName = string.Equals(line[5], "Attack", StringComparison.OrdinalIgnoreCase) ? "Auto Attack" : line[5];

        if (string.IsNullOrEmpty(sourceName) || string.IsNullOrEmpty(skillName))
            return;

        // Register entity ID → name from every ability line so pet-to-owner
        // resolution works even if the owner's Type 03 was missed.
        var sourceId = line[2];
        if (!string.IsNullOrEmpty(sourceId) && !string.IsNullOrEmpty(sourceName))
        {
            lock (syncLock)
                entityIdToName[sourceId] = sourceName;
        }

        // Resolve pet-to-owner via Type 03 entity ID mapping.
        string? petOwnerName = null;
        string? petEntityName = null;
        if (!string.IsNullOrEmpty(sourceId))
        {
            lock (syncLock)
            {
                if (petToOwnerId.TryGetValue(sourceId, out var ownerId)
                    && entityIdToName.TryGetValue(ownerId, out var ownerName))
                {
                    petOwnerName = ownerName;
                    petEntityName = sourceName;
                }
            }
        }

        // Ground-effect burst entity: when a ground entity (e.g. "Earthly Star")
        // deals Type 21/22 damage, resolve the owner from the name-based map.
        if (petOwnerName == null && GroundEffectEntityNames.Contains(sourceName))
        {
            lock (syncLock)
            {
                if (groundEffectEntityOwners.TryGetValue(sourceName, out var owner))
                {
                    petOwnerName = owner;
                    petEntityName = sourceName;
                }
            }
        }
        // When a player casts a ground-effect placement skill, record the mapping.
        else if (petOwnerName == null && GroundEffectEntityNames.Contains(skillName))
        {
            lock (syncLock)
            {
                groundEffectEntityOwners[skillName] = sourceName;
            }
        }

        // Separate item uses (item_XXXX) into their own tracking.
        if (skillName.StartsWith("item_", StringComparison.OrdinalIgnoreCase))
        {
            lock (syncLock)
            {
                RecordItemEvent(sourceName, skillName, targetName);
            }
            return;
        }

        // Pre-register ground-effect DoTs so the first tick is attributed
        // correctly even if the status gain (type 26) arrives after the tick.
        statusTracker?.NotifyGroundEffectSkillUsed(sourceName, skillName);

        var damageType = SkillDamageType.Unknown;
        uint actionId = 0;
        if (line.Length > 4 && uint.TryParse(line[4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out actionId))
            damageType = LookupDamageType(actionId);

        // Scan all 8 effect pairs (fields 8-23).
        // A single ability can have both damage and healing in different pairs
        // (e.g. drain abilities like Souleater, Energy Drain).
        long dmgAmount = 0;
        byte dmgSeverity = 0;
        int dmgBonusPercent = -1;
        long healAmount = 0;
        byte healSeverity = 0;
        for (int i = 0; i < 8; i++)
        {
            int flagIdx = 8 + i * 2;
            int valIdx = flagIdx + 1;
            if (valIdx >= line.Length)
                break;

            var result = DecodeEffect(line[flagIdx], line[valIdx]);
            if (result.Amount <= 0)
                continue;

            if (result.EffectType == 4)
            {
                if (healAmount == 0)
                {
                    healAmount = result.Amount;
                    healSeverity = result.Severity;
                }
            }
            else if (dmgAmount == 0)
            {
                dmgAmount = result.Amount;
                dmgSeverity = result.Severity;
                dmgBonusPercent = result.BonusPercent;
            }
        }

        // Scan for 0x0E/0x0F status-application effects to extract low-byte
        // refinement data (damage lowbyte + crit lowbyte) for DoT simulation,
        // and calibrate per-source damage-per-potency-point coefficients from
        // DoT initial hits.
        if (config.DotCalcMode != DotCalcMode.Iinact)
        {
            for (int i = 0; i < 8; i++)
            {
                int flagIdx = 8 + i * 2;
                int valIdx = flagIdx + 1;
                if (valIdx >= line.Length)
                    break;

                if (string.IsNullOrEmpty(line[flagIdx]) || string.IsNullOrEmpty(line[valIdx]))
                    continue;

                if (!uint.TryParse(line[flagIdx], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var sFlags))
                    continue;
                if (!uint.TryParse(line[valIdx], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var sRaw))
                    continue;

                var sEffectType = (byte)(sFlags & 0xFF);
                // 0x0E = status applied to target, 0x0F = status applied to caster
                if (sEffectType != 0x0E && sEffectType != 0x0F)
                    continue;

                // Upper 16 bits of the value field contain the status ID.
                var appliedStatusId = (uint)((sRaw >> 16) & 0xFFFF);
                if (appliedStatusId == 0)
                    continue;

                // Byte 1 of flags = damage lowbyte, Byte 2 = crit lowbyte
                var damageLB = (byte)((sFlags >> 8) & 0xFF);
                var critLB = (byte)((sFlags >> 16) & 0xFF);

                // Determine the target of the status: 0x0E = targetName, 0x0F = sourceName (self-buff)
                var statusTarget = sEffectType == 0x0E ? targetName : sourceName;
                if (string.IsNullOrEmpty(statusTarget))
                    continue;

                // Refined mode: store low-byte data for per-application refinement.
                if (config.DotCalcMode != DotCalcMode.Iinact)
                {
                    lock (syncLock)
                    {
                        pendingLowBytes[(sourceName, statusTarget, appliedStatusId)] = (damageLB, critLB);
                    }
                }

                // Calibrate per-potency coefficient from DoT initial hits.
                // If this ability dealt direct damage AND applied a known DoT with
                // a catalogued initial hit potency, use it to derive the coefficient.
                if (dmgAmount > 0)
                {
                    var initialPot = DotPotencyTable.GetInitialHitPotency(appliedStatusId);
                    if (initialPot > 0)
                    {
                        lock (syncLock)
                        {
                            CalibrateFromDotHit(sourceName, dmgAmount, dmgSeverity, initialPot);
                        }
                    }
                }
            }
        }

        // Track positional hits/misses for known melee positional actions.
        // Uses CSV lookup table approach inspired by DamageInfoPlugin:
        // https://github.com/perchbirdd/DamageInfoPlugin
        if (dmgAmount > 0 && dmgBonusPercent >= 0 && positionalTable.IsPositional(actionId))
        {
            lock (syncLock)
            {
                if (positionalTable.IsPositionalMiss(actionId, dmgBonusPercent))
                    positionalMissCounts[sourceName] = positionalMissCounts.GetValueOrDefault(sourceName) + 1;
                else
                    positionalHitCounts[sourceName] = positionalHitCounts.GetValueOrDefault(sourceName) + 1;
            }
        }

        // Count Leg Sweep / Low Blow uses regardless of whether they deal damage.
        if (string.Equals(skillName, "Leg Sweep", StringComparison.OrdinalIgnoreCase)
            || string.Equals(skillName, "Low Blow", StringComparison.OrdinalIgnoreCase))
        {
            lock (syncLock)
                stunCounts[sourceName] = stunCounts.GetValueOrDefault(sourceName) + 1;
        }

        if (dmgAmount <= 0 && healAmount <= 0)
            return;

        // Pet-sourced skills go into separate pet dictionaries so they appear
        // as a named category in the skill breakdown instead of inline.
        if (petOwnerName != null && petEntityName != null)
        {
            ServiceManager.LogDebug(LogChannel.PetDebug, $"[PetDebug] PetAccum owner={petOwnerName} pet={petEntityName} skill={skillName} dmg={dmgAmount} heal={healAmount}");
            lock (syncLock)
            {
                if (dmgAmount > 0)
                {
                    AccumulatePetSkill(petDamageData, petOwnerName, petEntityName, skillName, dmgAmount, dmgSeverity, damageType);
                    RecordEvent(petOwnerName, skillName, dmgAmount, false, dmgSeverity, targetName);

                    if (!string.IsNullOrEmpty(targetName))
                        RecordDamageTakenEvent(targetName, skillName, dmgAmount, dmgSeverity);
                }
                if (healAmount > 0)
                {
                    AccumulatePetSkill(petHealData, petOwnerName, petEntityName, skillName, healAmount, healSeverity, damageType);
                    RecordEvent(petOwnerName, skillName, healAmount, true, healSeverity, targetName);
                }
            }

            if (dmgAmount > 0 || healAmount > 0)
                graphTracker?.RecordLogLineEvent(petOwnerName, dmgAmount, healAmount);

            return;
        }

        lock (syncLock)
        {
            if (dmgAmount > 0)
            {
                AccumulateSkill(damageData, sourceName, skillName, dmgAmount, dmgSeverity, damageType);
                RecordEvent(sourceName, skillName, dmgAmount, false, dmgSeverity, targetName);

                if (!string.IsNullOrEmpty(targetName))
                    RecordDamageTakenEvent(targetName, skillName, dmgAmount, dmgSeverity);

                // Feed per-combatant stats for DoT/HoT tick simulation (exclude auto-attacks).
                if (!string.Equals(skillName, "Auto Attack", StringComparison.OrdinalIgnoreCase))
                    AccumulateCombatantStats(sourceName, dmgAmount, dmgSeverity);
            }
            if (healAmount > 0)
            {
                AccumulateSkill(healData, sourceName, skillName, healAmount, healSeverity, damageType);
                RecordEvent(sourceName, skillName, healAmount, true, healSeverity, targetName);
            }
        }

        // Feed high-resolution damage/heal totals into the graph tracker
        // outside the skill lock to avoid nested locking.
        if (dmgAmount > 0 || healAmount > 0)
            graphTracker?.RecordLogLineEvent(sourceName, dmgAmount, healAmount);
    }

    private void AccumulateSkill(Dictionary<string, Dictionary<string, SkillAccum>> store,
        string sourceName, string skillName, long amount, byte severity, SkillDamageType damageType)
    {
        bool isCrit = (severity & CritFlag) != 0;
        bool isDirectHit = (severity & DirectHitFlag) != 0;
        bool isCritDirectHit = isCrit && isDirectHit;

        if (!store.TryGetValue(sourceName, out var skills))
        {
            skills = new Dictionary<string, SkillAccum>();
            store[sourceName] = skills;
        }

        if (!skills.TryGetValue(skillName, out var existing))
            existing = default;

        existing.Amount += amount;
        existing.Hits++;
        if (isCritDirectHit)
            existing.CritDirectHits++;
        else if (isCrit)
            existing.Crits++;
        else if (isDirectHit)
            existing.DirectHits++;

        if (existing.DamageType == SkillDamageType.Unknown && damageType != SkillDamageType.Unknown)
            existing.DamageType = damageType;

        skills[skillName] = existing;
    }

    private void AccumulatePetSkill(Dictionary<string, Dictionary<string, Dictionary<string, SkillAccum>>> store,
        string ownerName, string petName, string skillName, long amount, byte severity, SkillDamageType damageType)
    {
        if (!store.TryGetValue(ownerName, out var pets))
        {
            pets = new Dictionary<string, Dictionary<string, SkillAccum>>(StringComparer.OrdinalIgnoreCase);
            store[ownerName] = pets;
        }

        if (!pets.TryGetValue(petName, out var skills))
        {
            skills = new Dictionary<string, SkillAccum>(StringComparer.OrdinalIgnoreCase);
            pets[petName] = skills;
        }

        bool isCrit = (severity & CritFlag) != 0;
        bool isDirectHit = (severity & DirectHitFlag) != 0;
        bool isCritDirectHit = isCrit && isDirectHit;

        if (!skills.TryGetValue(skillName, out var existing))
            existing = default;

        existing.Amount += amount;
        existing.Hits++;
        if (isCritDirectHit)
            existing.CritDirectHits++;
        else if (isCrit)
            existing.Crits++;
        else if (isDirectHit)
            existing.DirectHits++;

        if (existing.DamageType == SkillDamageType.Unknown && damageType != SkillDamageType.Unknown)
            existing.DamageType = damageType;

        skills[skillName] = existing;
    }

    /// <summary>
    /// Accumulate per-combatant damage stats for DoT/HoT tick simulation.
    /// Strips crit/DH from observed damage to estimate the base per-potency
    /// multiplier, tracks running crit/DH rates, and builds DH-stripped pools
    /// for dynamic crit multiplier estimation.
    /// Must be called under syncLock.
    /// </summary>
    private void AccumulateCombatantStats(string sourceName, long amount, byte severity)
    {
        bool isCrit = (severity & CritFlag) != 0;
        bool isDH = (severity & DirectHitFlag) != 0;

        if (!combatantDotStats.TryGetValue(sourceName, out var stats))
            stats = default;

        // Use dynamic crit multiplier when available, otherwise fall back to default.
        var critMulti = stats.DynamicCritMulti > 0 ? stats.DynamicCritMulti : DefaultCritMulti;

        // Strip crit/DH multipliers to estimate base damage
        double baseDmg = amount;
        if (isCrit) baseDmg /= critMulti;
        if (isDH) baseDmg /= 1.25;

        // Outlier filter: exclude hits outside 30%-300% of running average
        // to avoid potency spikes from skewing the per-potency estimate.
        // Skip filter when under 10 samples (similar to ACT's <50 swings rule).
        if (stats.TotalHits >= DotMinHitsForOutlierFilter)
        {
            var currentAvg = stats.AverageBaseDmgPerHit;
            if (currentAvg > 0 && (baseDmg < currentAvg * DotOutlierLowThreshold || baseDmg > currentAvg * DotOutlierHighThreshold))
                return;
        }

        stats.TotalBaseDamage += baseDmg;
        stats.TotalHits++;
        if (isCrit) stats.CritHits++;
        if (isDH) stats.DHHits++;

        // DH-stripped pools for dynamic crit multiplier estimation.
        // Crit damage is preserved so we can derive critMulti = avgCrit / avgNonCrit.
        double dhStripped = amount;
        if (isDH) dhStripped /= 1.25;
        if (isCrit)
        {
            stats.CritDHStripped += dhStripped;
            stats.CritCountForMulti++;
        }
        else
        {
            stats.NonCritDHStripped += dhStripped;
            stats.NonCritCount++;
        }

        combatantDotStats[sourceName] = stats;
    }

    /// <summary>
    /// Calibrate per-source damage-per-potency-point coefficient from a DoT initial hit.
    /// When we see a type 21/22 line that deals damage AND applies a known DoT whose
    /// initial hit potency is in DotPotencyTable, we can derive a clean coefficient.
    /// Must be called under syncLock.
    /// </summary>
    private void CalibrateFromDotHit(string sourceName, long amount, byte severity, int initialPotency)
    {
        if (initialPotency <= 0 || amount <= 0)
            return;

        if (!combatantDotStats.TryGetValue(sourceName, out var stats))
            stats = default;

        var critMulti = stats.DynamicCritMulti > 0 ? stats.DynamicCritMulti : DefaultCritMulti;

        double baseDmg = amount;
        if ((severity & CritFlag) != 0) baseDmg /= critMulti;
        if ((severity & DirectHitFlag) != 0) baseDmg /= 1.25;

        stats.CalibrationSum += baseDmg / initialPotency;
        stats.CalibrationCount++;
        combatantDotStats[sourceName] = stats;
    }

    /// <summary>
    /// Calculate the simulated tick weight for a source's DoT/HoT on a target.
    /// Weight = baseDmgPerHit × tickPotency × expectedCritDHFactor.
    /// Used to proportionally distribute aggregate type-24 tick damage.
    /// Must be called under syncLock.
    /// </summary>
    private double CalculateTickWeight(string sourceName, uint statusId, bool isHoT,
        byte damageLowByte, byte critLowByte, bool hasLowByteData, double fallbackCoeff)
    {
        var potency = DotPotencyTable.GetTickPotency(statusId);
        if (potency <= 0)
            return 0;

        if (!combatantDotStats.TryGetValue(sourceName, out var stats) || !stats.HasData)
            return (fallbackCoeff > 0 ? fallbackCoeff : 1.0) * potency;

        // Use calibrated per-potency coefficient when available, otherwise
        // fall back to the shared coefficient from other calibrated sources.
        // IMPORTANT: AverageBaseDmgPerHit is NOT per-potency — using it raw
        // would create ~100x weight distortion vs calibrated sources.
        double estimatedTick;
        if (stats.HasCalibration)
            estimatedTick = stats.CalibratedCoeff * potency;
        else if (fallbackCoeff > 0)
            estimatedTick = fallbackCoeff * potency;
        else
            return potency; // No calibration anywhere — weight by potency alone

        // Refined mode: snap estimated tick to the nearest value whose LSB
        // matches the low-byte from the 0x0E status-application effect.
        if (config.DotCalcMode != DotCalcMode.Iinact && hasLowByteData && estimatedTick > 0)
        {
            estimatedTick = SnapToLowByte(estimatedTick, damageLowByte);
        }

        // Use dynamic crit multiplier when available.
        var critMulti = stats.DynamicCritMulti > 0 ? stats.DynamicCritMulti : DefaultCritMulti;

        // Expected crit/DH multiplier applied to periodic ticks.
        double critRate = stats.CritRate;

        // Refined mode: use per-application crit rate from low-byte when available
        // and within the reliable range (≤ 25.5%, byte overflow threshold).
        if (config.DotCalcMode != DotCalcMode.Iinact && hasLowByteData && critLowByte > 0)
        {
            var perAppCrit = critLowByte / 1000.0;
            if (perAppCrit <= 0.255)
                critRate = perAppCrit;
        }

        double critFactor = 1.0 + (critMulti - 1.0) * critRate;
        double dhFactor = isHoT ? 1.0 : 1.0 + 0.25 * stats.DHRate;

        return estimatedTick * critFactor * dhFactor;
    }

    /// <summary>
    /// Snap an estimated tick value to the nearest integer whose least significant
    /// byte matches <paramref name="lowByte"/>. Used by the Refined DoT simulation
    /// to leverage the damage low-byte from 0x0E status-application effects.
    /// </summary>
    private static double SnapToLowByte(double estimated, byte lowByte)
    {
        // The LSB constrains: result & 0xFF == lowByte.
        // Candidates are: base | lowByte, (base+256) | lowByte, (base-256) | lowByte
        int estInt = (int)estimated;
        int baseVal = estInt & ~0xFF; // clear bottom byte

        double bestDist = double.MaxValue;
        double bestVal = estimated;

        for (int offset = -256; offset <= 256; offset += 256)
        {
            int candidate = (baseVal + offset) | lowByte;
            if (candidate <= 0)
                continue;
            double dist = Math.Abs(candidate - estimated);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestVal = candidate;
            }
        }

        return bestVal;
    }

    private SkillDamageType LookupDamageType(uint actionId)
    {
        if (damageTypeCache.TryGetValue(actionId, out var cached))
            return cached;

        var result = SkillDamageType.Unknown;
        try
        {
            var sheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>();
            if (sheet != null)
            {
                var row = sheet.GetRowOrDefault(actionId);
                if (row.HasValue)
                {
                    // AttackType: 0=None, 1=Slashing, 2=Piercing, 3=Blunt,
                    // 4=Shooting, 5=Magic, 6+=other physical types
                    var attackType = row.Value.AttackType.RowId;
                    result = attackType switch
                    {
                        0 => SkillDamageType.Unknown,
                        5 => SkillDamageType.Magic,
                        _ => SkillDamageType.Physical,
                    };
                }
            }
        }
        catch (Exception ex)
        {
            ServiceManager.LogDebug(LogChannel.SkillTracker, $"Failed to look up damage type for action {actionId}: {ex.Message}");
        }

        damageTypeCache[actionId] = result;
        return result;
    }

    /// <summary>Hardcoded damage type overrides for status-based detonations (e.g. Wildfire).</summary>
    private static readonly Dictionary<uint, SkillDamageType> StatusDamageTypeOverrides = new()
    {
        { 2310, SkillDamageType.Physical }, // Wildfire (MCH)
    };

    private SkillDamageType LookupStatusDamageType(uint statusId)
    {
        return StatusDamageTypeOverrides.GetValueOrDefault(statusId, SkillDamageType.Unknown);
    }

    /// <summary>
    /// Resolve a status effect's display name from active statuses or Lumina.
    /// Used for non-DoT status detonations (e.g. Wildfire) that arrive via Type 24 lines.
    /// </summary>
    private string ResolveStatusName(uint statusId, string targetName)
    {
        if (statusTracker != null)
        {
            var statuses = statusTracker.GetActiveStatuses(targetName);
            foreach (var s in statuses)
            {
                if (s.StatusId == statusId)
                    return s.StatusName;
            }
        }

        try
        {
            var sheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Status>();
            if (sheet != null)
            {
                var row = sheet.GetRowOrDefault(statusId);
                if (row.HasValue)
                {
                    var name = row.Value.Name.ToString();
                    if (!string.IsNullOrEmpty(name))
                        return name;
                }
            }
        }
        catch { }

        return $"Status {statusId}";
    }

    public List<SkillEntry> GetSkills(string combatantName)
    {
        return BuildSkillList(damageData, dotTickData, petDamageData, combatantName, "DoT");
    }

    public List<SkillEntry> GetHealSkills(string combatantName)
    {
        return BuildSkillList(healData, hotTickData, petHealData, combatantName, "HoT");
    }

    public List<SkillUseEvent> GetSkillEvents(string combatantName)
    {
        lock (syncLock)
            return GetEventsWithFallback(skillEvents, seededEvents, combatantName);
    }

    public List<SkillUseEvent> GetDamageTakenEvents(string combatantName)
    {
        lock (syncLock)
            return GetEventsWithFallback(damageTakenEvents, seededDamageTakenEvents, combatantName);
    }

    public List<SkillUseEvent> GetItemEvents(string combatantName)
    {
        lock (syncLock)
            return GetEventsWithFallback(itemEvents, seededItemEvents, combatantName);
    }

    /// <summary>Returns a copy of live events, falling back to seeded historical events. Must be called under <see cref="syncLock"/>.</summary>
    private static List<SkillUseEvent> GetEventsWithFallback(
        Dictionary<string, List<SkillUseEvent>> live,
        Dictionary<string, List<SkillUseEvent>>? seeded,
        string combatantName)
    {
        if (live.TryGetValue(combatantName, out var events) && events.Count > 0)
            return new List<SkillUseEvent>(events);

        if (seeded != null
            && seeded.TryGetValue(combatantName, out var fallback)
            && fallback.Count > 0)
            return new List<SkillUseEvent>(fallback);

        return new List<SkillUseEvent>();
    }

    /// <summary>Increments a status-stack counter (skill issue or damage down). Must NOT be called under <see cref="syncLock"/>.</summary>
    private void IncrementStatusStackCount(
        string targetName,
        string statusName,
        string[] line,
        Dictionary<string, int> counts,
        Dictionary<(string, string), int> stacks)
    {
        int newStacks = 1;
        if (line.Length > 9 && int.TryParse(line[9], out var parsed) && parsed > 0)
            newStacks = parsed;

        lock (syncLock)
        {
            var key = (targetName.ToLowerInvariant(), statusName.ToLowerInvariant());
            var prevStacks = stacks.GetValueOrDefault(key);
            var delta = newStacks - prevStacks;
            counts[targetName] = counts.GetValueOrDefault(targetName) + Math.Max(delta, 1);
            stacks[key] = newStacks;
        }
    }

    private int GetCountLocked(Dictionary<string, int> dict, string combatantName)
    {
        lock (syncLock)
        {
            return dict.GetValueOrDefault(combatantName);
        }
    }

    public int GetStunCount(string combatantName) => GetCountLocked(stunCounts, combatantName);
    public int GetSkillIssueCount(string combatantName) => GetCountLocked(skillIssueCounts, combatantName);
    public int GetDamageDownCount(string combatantName) => GetCountLocked(damageDownCounts, combatantName);
    public int GetPositionalHits(string combatantName) => GetCountLocked(positionalHitCounts, combatantName);
    public int GetPositionalMisses(string combatantName) => GetCountLocked(positionalMissCounts, combatantName);

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

    private void RecordEvent(string combatantName, string skillName, long amount, bool isHeal, byte severity,
        string? targetName = null, bool isDoTTick = false, bool isHoTTick = false)
    {
        if (!skillEvents.TryGetValue(combatantName, out var events))
        {
            events = new List<SkillUseEvent>();
            skillEvents[combatantName] = events;
        }

        events.Add(new SkillUseEvent
        {
            TimeSec = timer?.ElapsedSeconds ?? 0f,
            SkillName = skillName,
            TargetName = targetName,
            Amount = amount,
            IsHeal = isHeal,
            IsCrit = (severity & CritFlag) != 0,
            IsDirectHit = (severity & DirectHitFlag) != 0,
            IsDoTTick = isDoTTick,
            IsHoTTick = isHoTTick,
        });
    }

    /// <summary>
    /// Retroactively tag the most recent skill event as a DoT/HoT application.
    /// Called by StatusTracker when a GainsEffect (type 26) arrives for a known DoT/HoT.
    /// Sets IsDoTApplication/IsHoTApplication on the event for graph/timeline highlighting.
    /// </summary>
    public void MarkLastEventAsApplication(string combatantName, bool isDoT, bool isHoT)
    {
        lock (syncLock)
        {
            if (!skillEvents.TryGetValue(combatantName, out var events) || events.Count == 0)
                return;

            var now = timer?.ElapsedSeconds ?? 0f;
            const int maxScan = 10;
            const float maxAge = 3.0f;

            int start = Math.Max(0, events.Count - maxScan);
            for (int i = events.Count - 1; i >= start; i--)
            {
                var evt = events[i];

                if (evt.IsDoTTick || evt.IsHoTTick || evt.IsDoTApplication || evt.IsHoTApplication)
                    continue;

                if (now - evt.TimeSec > maxAge)
                    break;

                // Tag the first eligible event we find
                events[i] = evt with
                {
                    IsDoTApplication = isDoT || evt.IsDoTApplication,
                    IsHoTApplication = isHoT || evt.IsHoTApplication,
                };
                return;
            }
        }
    }

    private void RecordDamageTakenEvent(string targetName, string skillName, long amount, byte severity)
    {
        if (!damageTakenEvents.TryGetValue(targetName, out var events))
        {
            events = new List<SkillUseEvent>();
            damageTakenEvents[targetName] = events;
        }

        events.Add(new SkillUseEvent
        {
            TimeSec = timer?.ElapsedSeconds ?? 0f,
            SkillName = skillName,
            Amount = amount,
            IsHeal = false,
            IsCrit = (severity & CritFlag) != 0,
            IsDirectHit = (severity & DirectHitFlag) != 0,
        });
    }

    private void RecordItemEvent(string combatantName, string skillName, string? targetName)
    {
        if (!itemEvents.TryGetValue(combatantName, out var events))
        {
            events = new List<SkillUseEvent>();
            itemEvents[combatantName] = events;
        }

        events.Add(new SkillUseEvent
        {
            TimeSec = timer?.ElapsedSeconds ?? 0f,
            SkillName = skillName,
            TargetName = targetName,
            Amount = 0,
            IsHeal = false,
        });
    }

    private List<SkillEntry> BuildSkillList(
        Dictionary<string, Dictionary<string, SkillAccum>> store,
        Dictionary<string, Dictionary<string, SkillAccum>> tickStore,
        Dictionary<string, Dictionary<string, Dictionary<string, SkillAccum>>> petStore,
        string combatantName,
        string tickLabel)
    {
        lock (syncLock)
        {
            store.TryGetValue(combatantName, out var skills);
            tickStore.TryGetValue(combatantName, out var ticks);

            var list = new List<SkillEntry>();

            if (skills != null)
            {
                foreach (var kv in skills)
                {
                    var a = kv.Value;
                    var entry = new SkillEntry
                    {
                        Name = kv.Key,
                        TotalDamage = a.Amount,
                        HitCount = a.Hits,
                        DamageType = a.DamageType,
                    };
                    if (a.Hits > 0)
                    {
                        entry.CritPct = (double)(a.Crits + a.CritDirectHits) / a.Hits * 100.0;
                        entry.DirectHitPct = (double)(a.DirectHits + a.CritDirectHits) / a.Hits * 100.0;
                        entry.CritDirectHitPct = (double)a.CritDirectHits / a.Hits * 100.0;
                    }

                    if (ticks != null && ticks.TryGetValue(kv.Key, out var tickAccum) && tickAccum.Hits > 0)
                    {
                        var tickEntry = new SkillEntry
                        {
                            Name = $"{kv.Key} ({tickLabel})",
                            TotalDamage = tickAccum.Amount,
                            HitCount = tickAccum.Hits,
                            DamageType = tickAccum.DamageType,
                        };
                        if (tickAccum.Hits > 0)
                        {
                            tickEntry.CritPct = (double)(tickAccum.Crits + tickAccum.CritDirectHits) / tickAccum.Hits * 100.0;
                            tickEntry.DirectHitPct = (double)(tickAccum.DirectHits + tickAccum.CritDirectHits) / tickAccum.Hits * 100.0;
                            tickEntry.CritDirectHitPct = (double)tickAccum.CritDirectHits / tickAccum.Hits * 100.0;
                        }
                        entry.SubEntries = new List<SkillEntry> { tickEntry };
                    }

                    list.Add(entry);
                }
            }

            // Merge pet categories: each pet becomes a top-level entry with its skills as sub-entries.
            if (petStore.TryGetValue(combatantName, out var pets))
            {
                ServiceManager.LogDebug(LogChannel.PetDebug, $"[PetDebug] BuildSkillList found {pets.Count} pet(s) for {combatantName}");
                foreach (var (petName, petSkills) in pets)
                {
                    long petTotal = 0;
                    int petHits = 0;
                    int petCrits = 0;
                    int petDirectHits = 0;
                    int petCritDirectHits = 0;
                    var subEntries = new List<SkillEntry>();

                    foreach (var (sName, acc) in petSkills)
                    {
                        petTotal += acc.Amount;
                        petHits += acc.Hits;
                        petCrits += acc.Crits + acc.CritDirectHits;
                        petDirectHits += acc.DirectHits + acc.CritDirectHits;
                        petCritDirectHits += acc.CritDirectHits;

                        var sub = new SkillEntry
                        {
                            Name = sName,
                            TotalDamage = acc.Amount,
                            HitCount = acc.Hits,
                            DamageType = acc.DamageType,
                        };
                        if (acc.Hits > 0)
                        {
                            sub.CritPct = (double)(acc.Crits + acc.CritDirectHits) / acc.Hits * 100.0;
                            sub.DirectHitPct = (double)(acc.DirectHits + acc.CritDirectHits) / acc.Hits * 100.0;
                            sub.CritDirectHitPct = (double)acc.CritDirectHits / acc.Hits * 100.0;
                        }
                        subEntries.Add(sub);
                    }

                    var petEntry = new SkillEntry
                    {
                        Name = petName,
                        TotalDamage = petTotal,
                        HitCount = petHits,
                        SubEntries = subEntries.OrderByDescending(s => s.TotalDamage).ToList(),
                    };
                    if (petHits > 0)
                    {
                        petEntry.CritPct = (double)petCrits / petHits * 100.0;
                        petEntry.DirectHitPct = (double)petDirectHits / petHits * 100.0;
                        petEntry.CritDirectHitPct = (double)petCritDirectHits / petHits * 100.0;
                    }
                    list.Add(petEntry);
                }
            }

            list.Sort((a, b) => b.TotalDamage.CompareTo(a.TotalDamage));

            var total = list.Sum(s => s.TotalDamage);
            if (total > 0)
            {
                foreach (var s in list)
                {
                    s.DamagePercent = (double)s.TotalDamage / total * 100.0;
                    if (s.SubEntries != null)
                    {
                        foreach (var sub in s.SubEntries)
                            sub.DamagePercent = (double)sub.TotalDamage / total * 100.0;
                    }
                }
            }

            return list;
        }
    }

    /// Decode an ability effect from FFXIV network log line fields.
    /// See: https://github.com/OverlayPlugin/cactbot/blob/main/docs/LogGuide.md#ability-damage
    private static (long Amount, byte Severity, byte EffectType, int BonusPercent) DecodeEffect(string flagsHex, string valueHex)
    {
        if (string.IsNullOrEmpty(flagsHex) || string.IsNullOrEmpty(valueHex))
            return (0, 0, 0, -1);

        if (!uint.TryParse(flagsHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var flags))
            return (0, 0, 0, -1);
        if (!uint.TryParse(valueHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var raw))
            return (0, 0, 0, -1);

        // Low byte of flags is the effect type:
        //   0x03 = damage dealt, 0x04 = heal, 0x05 = blocked damage, 0x06 = parried damage
        var effectType = (byte)(flags & 0xFF);
        if (effectType != 3 && effectType != 4 && effectType != 5 && effectType != 6)
            return (0, 0, 0, -1);

        // Second byte of flags is the severity (crit/DH):
        //   0x20 = crit, 0x40 = direct hit, 0x60 = crit direct hit
        var severity = (byte)((flags >> 8) & 0xFF);

        // Bonus percent: DamageInfoPlugin reads EffectEntry.param2 (byte 3 of the
        // 8-byte struct), which maps to the top byte of the FLAGS field in ACT logs.
        // The 8-byte EffectEntry is split as flagsHex=[type,param0,param1,param2] and
        // valueHex=[mult,flags,value]. param2 = (flagsHex >> 24) & 0xFF.
        // https://github.com/perchbirdd/DamageInfoPlugin
        int bonusPercent = (int)((flags >> 24) & 0xFF);

        // Value bytes (left-extended to 4 bytes): ABCD
        // Normal: damage is upper 16 bits (AB).
        // "A lot" (0x4000 mask in value field): damage reassembled as D-A-B.
        long amount;
        if ((raw & 0x4000) != 0)
        {
            var a = (raw >> 24) & 0xFF;
            var b = (raw >> 16) & 0xFF;
            var d = raw & 0xFF;
            amount = (long)((d << 16) | (a << 8) | b);
        }
        else
        {
            amount = (long)((raw >> 16) & 0xFFFF);
        }

        return (amount, severity, effectType, bonusPercent);
    }

    /// <summary>
    /// Parse ACT log line types 26 (GainsEffect) and 30 (LosesEffect)
    /// and forward to the StatusTracker for DoT/HoT lifecycle tracking.
    ///
    /// IINACT field layout:
    ///   [0]=type, [1]=timestamp, [2]=statusId(hex), [3]=statusName,
    ///   [4]=duration(float), [5]=sourceId(hex), [6]=sourceName,
    ///   [7]=targetId(hex), [8]=targetName, [9]=stacks, [10]=targetHP, ...
    /// </summary>
    private void ProcessStatusLine(string type, string[] line)
    {
        if (statusTracker == null)
            return;

        if (line.Length < 9)
            return;

        // IINACT field layout for type 26/30:
        //   [0]=type, [1]=timestamp, [2]=statusId(hex), [3]=statusName,
        //   [4]=duration, [5]=sourceId, [6]=sourceName,
        //   [7]=targetId, [8]=targetName, [9]=count, ...
        var statusIdHex = line[2];
        var statusName = line[3];
        var sourceName = line[6];
        var targetName = line[8];

        if (string.IsNullOrEmpty(statusIdHex))
            return;

        if (!uint.TryParse(statusIdHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var statusId))
            return;

        if (type == "26")
        {
            // GainsEffect — parse duration from field [4]
            float duration = 0f;
            if (line.Length > 4)
                float.TryParse(line[4], NumberStyles.Float, CultureInfo.InvariantCulture, out duration);

            // Consume pending low-byte refinement data captured from the Type 21/22
            // ability line that applied this status.
            byte damageLB = 0, critLB = 0;
            bool hasLB = false;
            lock (syncLock)
            {
                var lbKey = (sourceName, targetName, statusId);
                if (pendingLowBytes.Remove(lbKey, out var lb))
                {
                    damageLB = lb.DamageLowByte;
                    critLB = lb.CritLowByte;
                    hasLB = true;
                }
            }

            statusTracker.OnStatusGained(sourceName, targetName, statusId, statusName, duration,
                damageLB, critLB, hasLB);

            if (SkillIssueNames.Contains(statusName))
                IncrementStatusStackCount(targetName, statusName, line, skillIssueCounts, skillIssueStacks);

            if (DamageDownNames.Contains(statusName))
                IncrementStatusStackCount(targetName, statusName, line, damageDownCounts, damageDownStacks);
        }
        else if (type == "30")
        {
            var removalTime = timer?.ElapsedSeconds ?? 0f;
            statusTracker.OnStatusLost(sourceName, targetName, statusId, removalTime);

            if (SkillIssueNames.Contains(statusName))
            {
                lock (syncLock)
                    skillIssueStacks.Remove((targetName.ToLowerInvariant(), statusName.ToLowerInvariant()));
            }

            if (DamageDownNames.Contains(statusName))
            {
                lock (syncLock)
                    damageDownStacks.Remove((targetName.ToLowerInvariant(), statusName.ToLowerInvariant()));
            }
        }
    }

    /// <summary>
    /// Parse ACT log line type 24 (DoTHoT) — periodic damage/heal ticks.
    ///
    /// Type 24 lines are AGGREGATED — one line per tick combining damage from
    /// ALL DoTs/HoTs on the target from ALL sources. We simulate individual tick
    /// amounts using a per-potency multiplier approach inspired by the
    /// FFXIV_ACT_Plugin (see wiki link below) and distribute proportionally.
    ///
    /// https://github.com/ravahn/FFXIV_ACT_Plugin/wiki/DoT---HoT-Simulation-details
    ///
    /// Verified IINACT field layout:
    ///   [0]=type, [1]=timestamp, [2]=targetId(hex), [3]=targetName,
    ///   [4]="DoT"/"HoT", [5]=effectId(?), [6]=amount(hex),
    ///   [7]=targetCurrentHP, [8]=targetMaxHP, ...
    ///   [17]=sourceId(hex), [18]=sourceName, ...
    /// </summary>
    private void ProcessDoTHoTLine(string[] line)
    {
        // Need at least 7 fields to read amount; 19 to read sourceName.
        if (line.Length < 7)
            return;

        var targetName = line[3];
        var dotOrHot = line[4];       // "DoT" or "HoT"
        var effectIdHex = line[5];    // Non-zero for ground-effect DoTs/HoTs
        var amountHex = line[6];
        var sourceName = line.Length > 18 ? line[18] : string.Empty;

        if (string.IsNullOrEmpty(amountHex))
            return;

        if (!long.TryParse(amountHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
            return;

        bool isHoT = string.Equals(dotOrHot, "HoT", StringComparison.OrdinalIgnoreCase);
        bool isDoT = !isHoT;

        dotHotLineCount++;
        if (isDoT) dotLineCount++;

        // Ground-effect lines have a non-zero effectId and carry damage for
        // that one specific ground effect only.  Regular (aggregate) lines
        // have effectId=0 and carry the combined damage of ALL status-effect
        // DoTs/HoTs on the target from ALL sources.
        long eid = 0;
        bool isGroundEffect = !string.IsNullOrEmpty(effectIdHex)
                              && effectIdHex != "0"
                              && long.TryParse(effectIdHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out eid)
                              && eid != 0;

        var sourceSkills = new Dictionary<string, List<(string Name, uint StatusId, byte DamageLowByte, byte CritLowByte, bool HasLowByteData)>>(StringComparer.OrdinalIgnoreCase);

#if DEBUG
        if (isGroundEffect) dotGroundEffectCount++;
        else if (isDoT) dotAggregateCount++;
#endif

        if (statusTracker != null)
        {
            if (isGroundEffect)
            {
                // Non-DoT status detonation (e.g. Wildfire): attribute as
                // direct damage from the source, not as a periodic tick.
                var effectStatusId = (uint)eid;
                if (!statusTracker.IsDoT(effectStatusId)
                    && !statusTracker.IsHoT(effectStatusId)
                    && !statusTracker.IsGroundEffectDot(effectStatusId))
                {
                    if (isDoT && !string.IsNullOrEmpty(sourceName))
                    {
                        var skillName = ResolveStatusName(effectStatusId, targetName);
                        var damageType = LookupStatusDamageType(effectStatusId);
                        lock (syncLock)
                        {
                            AccumulateSkill(damageData, sourceName, skillName, amount, 0, damageType);
                            RecordEvent(sourceName, skillName, amount, false, 0);
                            if (!string.IsNullOrEmpty(targetName))
                                RecordDamageTakenEvent(targetName, skillName, amount, 0);
                        }
                        graphTracker?.RecordLogLineEvent(sourceName, amount, 0);
                    }
                    return;
                }

                // Ground-effect line: attribute only to the source's ground
                // effect skill.  The sourceName IS meaningful here.
                if (isDoT && !string.IsNullOrEmpty(sourceName))
                {
                    var groundEffects = statusTracker.GetActiveGroundEffectDots(sourceName);
                    foreach (var (skillName, statusId2) in groundEffects)
                    {
                        if (!sourceSkills.TryGetValue(sourceName, out var skills))
                        {
                            skills = new List<(string, uint, byte, byte, bool)>();
                            sourceSkills[sourceName] = skills;
                        }
                        if (!skills.Any(e => e.Name == skillName))
                            skills.Add((skillName, statusId2, 0, 0, false));
                    }
                }
            }
            else
            {
                // Aggregate line: collect ALL sources' status-effect DoTs/HoTs
                // on this target.  The sourceName field on aggregate lines
                // rotates arbitrarily and must NOT be used to filter.
                var statuses = statusTracker.GetActiveStatuses(targetName);
#if DEBUG
                if (isDoT && dotAggregateCount <= 5)
                    ServiceManager.LogDebug(LogChannel.DoTDiag, $"[DoTDiag] Aggregate #{dotAggregateCount}: target={targetName} amt={amount} activeStatuses={statuses.Count} dotStatuses={statuses.Count(s => s.IsDoT)}");
#endif
                foreach (var s in statuses)
                {
                    if ((isDoT && s.IsDoT) || (isHoT && s.IsHoT))
                    {
                        var name = s.ApplyingActionName ?? s.StatusName;
                        if (!sourceSkills.TryGetValue(s.SourceName, out var skills))
                        {
                            skills = new List<(string, uint, byte, byte, bool)>();
                            sourceSkills[s.SourceName] = skills;
                        }
                        skills.Add((name, s.StatusId, s.DamageLowByte, s.CritLowByte, s.HasLowByteData));
                    }
                }

                // Grace period buffer for recently-removed statuses.
                var recentlyRemoved = statusTracker.GetRecentlyRemovedDoTs(targetName);
                foreach (var s in recentlyRemoved)
                {
                    if ((isDoT && s.IsDoT) || (isHoT && s.IsHoT))
                    {
                        var name = s.ApplyingActionName ?? s.StatusName;
                        if (sourceSkills.TryGetValue(s.SourceName, out var existingSkills)
                            && existingSkills.Any(e => e.Name == name))
                            continue;

                        if (!sourceSkills.TryGetValue(s.SourceName, out var skills))
                        {
                            skills = new List<(string, uint, byte, byte, bool)>();
                            sourceSkills[s.SourceName] = skills;
                        }
                        skills.Add((name, s.StatusId, s.DamageLowByte, s.CritLowByte, s.HasLowByteData));
                    }
                }
            }
        }

#if DEBUG
        if (sourceSkills.Count > 0 && isDoT) dotStatusFoundCount++;
#endif

        // Fallback: attribute to the named source with a generic label when
        // StatusTracker has no data (e.g. plugin started mid-encounter).
        if (sourceSkills.Count == 0)
        {
#if DEBUG
            dotFallbackCount++;
#endif
            var fallbackSource = !string.IsNullOrEmpty(sourceName) ? sourceName : targetName;
            sourceSkills[fallbackSource] = new List<(string, uint, byte, byte, bool)> { (isHoT ? "HoT" : "DoT", 0u, 0, 0, false) };
        }

        // Potency-weighted distribution: simulate each DoT/HoT's expected tick
        // amount using per-combatant stats and tick potency, then distribute the
        // aggregate damage proportionally to the simulated weights.
        var weightedSlots = new List<(string Source, string SkillName, double Weight)>();
        var sourceWeights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        lock (syncLock)
        {
            // Compute an average calibrated per-potency coefficient from sources
            // that have calibration data.  Sources without calibration will use
            // this shared coefficient so all weights stay in the same units.
            double calibSum = 0;
            int calibCount = 0;
            foreach (var (src, _) in sourceSkills)
            {
                if (combatantDotStats.TryGetValue(src, out var s) && s.HasCalibration)
                {
                    calibSum += s.CalibratedCoeff;
                    calibCount++;
                }
            }
            double fallbackCoeff = calibCount > 0 ? calibSum / calibCount : 0;

            // Phase 1: calculate weights for each source × skill slot.
            foreach (var (src, skills) in sourceSkills)
            {
                foreach (var (skillName, statusId, dLB, cLB, hasLB) in skills)
                {
                    var weight = CalculateTickWeight(src, statusId, isHoT, dLB, cLB, hasLB, fallbackCoeff);
                    weightedSlots.Add((src, skillName, weight));

                    sourceWeights[src] = sourceWeights.GetValueOrDefault(src) + weight;
                }
            }

            var totalWeight = weightedSlots.Sum(s => s.Weight);
            if (totalWeight <= 0) totalWeight = 1;

            // Phase 2: distribute the aggregate damage proportionally.
            long distributed = 0;
            for (int i = 0; i < weightedSlots.Count; i++)
            {
                var (src, skillName, weight) = weightedSlots[i];

                // Last slot gets remainder to ensure total matches exactly.
                long share;
                if (i == weightedSlots.Count - 1)
                    share = amount - distributed;
                else
                    share = (long)(amount * weight / totalWeight);

                distributed += share;
                if (share <= 0) continue;

                if (isDoT)
                {
                    AccumulateSkill(damageData, src, skillName, share, 0, SkillDamageType.Magic);
                    AccumulateSkill(dotTickData, src, skillName, share, 0, SkillDamageType.Magic);
                    RecordEvent(src, skillName, share, false, 0, isDoTTick: true);
#if DEBUG
                    dotTotalDamageDistributed += share;
#endif

                    if (!string.IsNullOrEmpty(targetName))
                        RecordDamageTakenEvent(targetName, skillName, share, 0);
                }
                else
                {
                    AccumulateSkill(healData, src, skillName, share, 0, SkillDamageType.Magic);
                    AccumulateSkill(hotTickData, src, skillName, share, 0, SkillDamageType.Magic);
                    RecordEvent(src, skillName, share, true, 0, isHoTTick: true);
                }
            }
        }

        // Feed into graph tracker — split per source proportionally to weights.
        var totalSourceWeight = sourceWeights.Sum(kv => kv.Value);
        if (totalSourceWeight <= 0) totalSourceWeight = 1;

        long graphDistributed = 0;
        var sourceList = sourceWeights.ToList();
        for (int i = 0; i < sourceList.Count; i++)
        {
            var (src, weight) = sourceList[i];
            long srcShare;
            if (i == sourceList.Count - 1)
                srcShare = amount - graphDistributed;
            else
                srcShare = (long)(amount * weight / totalSourceWeight);

            graphDistributed += srcShare;
            if (srcShare <= 0) continue;

            if (isDoT)
                graphTracker?.RecordLogLineEvent(src, srcShare, 0);
            else
                graphTracker?.RecordLogLineEvent(src, 0, srcShare);
        }
    }
}
