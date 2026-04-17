using DamageTerror.Helpers;

namespace DamageTerror.Jobs;

public static class JobRegistry
{
    private static readonly JobDefinitionBase[] AllDefinitions =
    [
        // Tanks
        new PLD(), new WAR(), new DRK(), new GNB(),
        // Healers
        new WHM(), new SCH(), new AST(), new SGE(),
        // Melee DPS
        new MNK(), new DRG(), new NIN(), new SAM(), new RPR(), new VPR(),
        // Ranged DPS
        new BRD(), new MCH(), new DNC(),
        // Caster DPS
        new BLM(), new SMN(), new RDM(), new PCT(), new BLU(),
        // Base Classes
        new GLA(), new MRD(), new CNJ(), new PGL(), new LNC(), new ARC(), new ROG(), new THM(), new ACN(),
        // Special
        new LMB(),
    ];

    // ── Lookup by abbreviation or full name (case-insensitive) ──
    private static readonly Dictionary<string, JobDefinitionBase> Lookup = BuildLookup();

    // ── Aggregated dictionaries ──
    private static readonly Dictionary<uint, int> AggregatedDotTickPotencies = BuildAggregatedDict(j => j.DotTickPotencies);
    private static readonly Dictionary<uint, int> AggregatedHotTickPotencies = BuildAggregatedDict(j => j.HotTickPotencies);
    private static readonly Dictionary<uint, int> AggregatedDotInitialHitPotencies = BuildAggregatedDict(j => j.DotInitialHitPotencies);
    private static readonly HashSet<uint> AggregatedKnownDotStatusIds = BuildAggregatedSet(j => j.KnownDotStatusIds);
    private static readonly HashSet<uint> AggregatedKnownHotStatusIds = BuildAggregatedSet(j => j.KnownHotStatusIds);
    private static readonly Dictionary<uint, string> AggregatedGroundEffectDots = BuildGroundEffectDots();

    // ── Role-grouped arrays ──
    public static readonly string[] TankJobs = GetAbbreviations(JobRole.Tank, baseClasses: false);
    public static readonly string[] HealerJobs = GetAbbreviations(JobRole.Healer, baseClasses: false);
    public static readonly string[] MeleeDpsJobs = GetAbbreviations(JobRole.MeleeDps, baseClasses: false);
    public static readonly string[] RangedDpsJobs = GetAbbreviations(JobRole.RangedDps, baseClasses: false);
    public static readonly string[] CasterDpsJobs = GetAbbreviations(JobRole.CasterDps, baseClasses: false);
    public static readonly string[] BaseClassJobs = AllDefinitions.Where(d => d.IsBaseClass).Select(d => d.Abbreviation).ToArray();
    public static readonly string[] AllAbbreviations = AllDefinitions.Select(d => d.Abbreviation).ToArray();

    // ── Identity lookups ──

    public static bool TryGet(string key, out JobDataTable.JobEntry entry)
    {
        if (!string.IsNullOrEmpty(key) && Lookup.TryGetValue(key, out var def))
        {
            entry = new JobDataTable.JobEntry(def.Abbreviation, def.FullName, def.Role, def.ClassJobId, def.DefaultColor, def.IsBaseClass);
            return true;
        }
        entry = default;
        return false;
    }

    public static JobRole GetRole(string job)
    {
        if (string.IsNullOrEmpty(job)) return JobRole.Default;
        return Lookup.TryGetValue(job, out var def) ? def.Role : JobRole.Default;
    }

    public static string GetFullName(string abbreviation)
    {
        if (string.IsNullOrEmpty(abbreviation)) return abbreviation;
        return Lookup.TryGetValue(abbreviation, out var def) ? def.FullName : abbreviation;
    }

    public static Vector4 GetDefaultColor(string job)
    {
        if (!string.IsNullOrEmpty(job) && Lookup.TryGetValue(job, out var def))
            return def.DefaultColor;
        return new(0.5f, 0.5f, 0.5f, 1.0f);
    }

    public static uint? GetClassJobId(string job)
    {
        if (!string.IsNullOrEmpty(job) && Lookup.TryGetValue(job, out var def) && def.ClassJobId > 0)
            return def.ClassJobId;
        return null;
    }

    // ── Potency lookups ──

    public static int GetTickPotency(uint statusId) =>
        AggregatedDotTickPotencies.TryGetValue(statusId, out var p)
            ? p
            : AggregatedHotTickPotencies.GetValueOrDefault(statusId, DotPotencyTable.DefaultPotency);

    public static int GetInitialHitPotency(uint statusId) =>
        AggregatedDotInitialHitPotencies.GetValueOrDefault(statusId, 0);

    // ── Status classification lookups ──

    public static HashSet<uint> GetKnownDotStatusIds() => AggregatedKnownDotStatusIds;
    public static HashSet<uint> GetKnownHotStatusIds() => AggregatedKnownHotStatusIds;
    public static Dictionary<uint, string> GetGroundEffectDotIds() => AggregatedGroundEffectDots;

    // ── Positional fallbacks ──

    public static IEnumerable<PositionalFallbackEntry> GetAllFallbackPositionals() =>
        AllDefinitions.SelectMany(d => d.FallbackPositionals);

    // ── Sample data lookups ──

    public static string GetMaxHitSkill(string job) =>
        Lookup.TryGetValue(job, out var def) ? def.MaxHitSkill : "Attack";

    public static string[] GetDamageSkillNames(string job) =>
        Lookup.TryGetValue(job, out var def) ? def.DamageSkillNames : ["Attack", "Auto-Attack"];

    public static string[] GetHealSkillNames(string job) =>
        Lookup.TryGetValue(job, out var def) ? def.HealSkillNames : ["Second Wind", "Bloodbath"];

    public static (uint Id, string Name, float Duration, bool IsHoT)[] GetJobBuffs(string job) =>
        Lookup.TryGetValue(job, out var def) ? def.SampleBuffs : [];

    public static (uint Id, string Name, float Duration, bool IsDot)[] GetJobDebuffs(string job) =>
        Lookup.TryGetValue(job, out var def) ? def.SampleDebuffs : [];

    // ── Build helpers ──

    private static Dictionary<string, JobDefinitionBase> BuildLookup()
    {
        var dict = new Dictionary<string, JobDefinitionBase>(AllDefinitions.Length * 2, StringComparer.OrdinalIgnoreCase);
        foreach (var def in AllDefinitions)
        {
            dict[def.Abbreviation] = def;
            dict[def.FullName.Replace(" ", "")] = def;
        }
        // Extra alias: "Limit Break" with space
        if (dict.TryGetValue("Lmb", out var lmb))
            dict["Limit Break"] = lmb;
        return dict;
    }

    private static string[] GetAbbreviations(JobRole role, bool baseClasses) =>
        AllDefinitions.Where(d => d.Role == role && d.IsBaseClass == baseClasses).Select(d => d.Abbreviation).ToArray();

    private static Dictionary<uint, int> BuildAggregatedDict(Func<JobDefinitionBase, IReadOnlyDictionary<uint, int>> selector)
    {
        var result = new Dictionary<uint, int>();
        foreach (var def in AllDefinitions)
        {
            foreach (var (key, value) in selector(def))
                result[key] = value;
        }
        return result;
    }

    private static HashSet<uint> BuildAggregatedSet(Func<JobDefinitionBase, IReadOnlySet<uint>> selector)
    {
        var result = new HashSet<uint>();
        foreach (var def in AllDefinitions)
        {
            foreach (var id in selector(def))
                result.Add(id);
        }
        return result;
    }

    private static Dictionary<uint, string> BuildGroundEffectDots()
    {
        var result = new Dictionary<uint, string>();
        foreach (var def in AllDefinitions)
        {
            foreach (var (key, value) in def.GroundEffectDots)
                result[key] = value;
        }
        return result;
    }
}
