
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
        new BLM(), new SMN(), new RDM(),
        new IdentityJob("Pct", "Pictomancer", JobRole.CasterDps, 42, new(0.9882353f, 0.57254905f, 0.88235295f, 1.0f)),
        new BLU(),
        // Base Classes
        new IdentityJob("Gla", "Gladiator", JobRole.Tank, 1, new(0.65882355f, 0.8235294f, 0.9019608f, 1.0f), true),
        new IdentityJob("Mrd", "Marauder", JobRole.Tank, 3, new(0.8117647f, 0.14901961f, 0.12941177f, 1.0f), true),
        new IdentityJob("Cnj", "Conjurer", JobRole.Healer, 6, new(1.0f, 0.9411765f, 0.8627451f, 1.0f), true),
        new IdentityJob("Pgl", "Pugilist", JobRole.MeleeDps, 2, new(0.8392157f, 0.6117647f, 0.0f, 1.0f), true),
        new IdentityJob("Lnc", "Lancer", JobRole.MeleeDps, 4, new(0.25490198f, 0.39215687f, 0.8039216f, 1.0f), true),
        new IdentityJob("Arc", "Archer", JobRole.RangedDps, 5, new(0.5686275f, 0.7294118f, 0.36862746f, 1.0f), true),
        new IdentityJob("Rog", "Rogue", JobRole.MeleeDps, 29, new(0.6862745f, 0.09803922f, 0.39215687f, 1.0f), true),
        new IdentityJob("Thm", "Thaumaturge", JobRole.CasterDps, 7, new(0.64705884f, 0.4745098f, 0.8392157f, 1.0f), true),
        new IdentityJob("Acn", "Arcanist", JobRole.CasterDps, 26, new(0.1764706f, 0.60784316f, 0.47058824f, 1.0f), true),
        // Crafters
        new IdentityJob("Crp", "Carpenter", JobRole.DoHL, 8, new(0.70f, 0.55f, 0.30f, 1.0f)),
        new IdentityJob("Bsm", "Blacksmith", JobRole.DoHL, 9, new(0.70f, 0.55f, 0.30f, 1.0f)),
        new IdentityJob("Arm", "Armorer", JobRole.DoHL, 10, new(0.70f, 0.55f, 0.30f, 1.0f)),
        new IdentityJob("Gsm", "Goldsmith", JobRole.DoHL, 11, new(0.70f, 0.55f, 0.30f, 1.0f)),
        new IdentityJob("Ltw", "Leatherworker", JobRole.DoHL, 12, new(0.70f, 0.55f, 0.30f, 1.0f)),
        new IdentityJob("Wvr", "Weaver", JobRole.DoHL, 13, new(0.70f, 0.55f, 0.30f, 1.0f)),
        new IdentityJob("Alc", "Alchemist", JobRole.DoHL, 14, new(0.70f, 0.55f, 0.30f, 1.0f)),
        new IdentityJob("Cul", "Culinarian", JobRole.DoHL, 15, new(0.70f, 0.55f, 0.30f, 1.0f)),
        // Gatherers
        new IdentityJob("Min", "Miner", JobRole.DoHL, 16, new(0.70f, 0.55f, 0.30f, 1.0f)),
        new IdentityJob("Btn", "Botanist", JobRole.DoHL, 17, new(0.70f, 0.55f, 0.30f, 1.0f)),
        new IdentityJob("Fsh", "Fisher", JobRole.DoHL, 18, new(0.70f, 0.55f, 0.30f, 1.0f)),
        // Special
        new IdentityJob("Lmb", "Limit Break", JobRole.LimitBreak, 0, new(0.5f, 0.5f, 0.5f, 1.0f)),
    ];

    // ── Lookup by abbreviation or full name (case-insensitive) ──
    private static readonly Dictionary<string, JobDefinitionBase> Lookup = BuildLookup();

    // ── Aggregated dictionaries ──
    private static readonly Dictionary<uint, int> AggregatedDotTickPotencies = BuildAggregatedDict(j => j.DotTickPotencies);
    private static readonly Dictionary<uint, int> AggregatedHotTickPotencies = BuildAggregatedDict(j => j.HotTickPotencies);
    private static readonly Dictionary<uint, int> AggregatedDotInitialHitPotencies = BuildAggregatedDict(j => j.DotInitialHitPotencies);
    private static readonly HashSet<uint> AggregatedKnownReflectStatusIds = BuildAggregatedSet(j => j.KnownReflectStatusIds);
    private static readonly Dictionary<uint, string> AggregatedGroundEffectDots = BuildAggregatedDict(j => j.GroundEffectDots);

    // ── Derived status classification sets (must init after the maps above) ──
    // Known HoTs are exactly the HoT-potency keys; known DoTs are the DoT-potency
    // keys minus ground-effect DoTs (whose self-buff status isn't a target debuff).
    private static readonly HashSet<uint> AggregatedKnownHotStatusIds = AggregatedHotTickPotencies.Keys.ToHashSet();
    private static readonly HashSet<uint> AggregatedKnownDotStatusIds =
        AggregatedDotTickPotencies.Keys.Where(id => !AggregatedGroundEffectDots.ContainsKey(id)).ToHashSet();

    // ── Role-grouped arrays ──
    public static readonly string[] TankJobs = GetAbbreviations(JobRole.Tank, baseClasses: false);
    public static readonly string[] HealerJobs = GetAbbreviations(JobRole.Healer, baseClasses: false);
    public static readonly string[] MeleeDpsJobs = GetAbbreviations(JobRole.MeleeDps, baseClasses: false);
    public static readonly string[] RangedDpsJobs = GetAbbreviations(JobRole.RangedDps, baseClasses: false);
    public static readonly string[] CasterDpsJobs = GetAbbreviations(JobRole.CasterDps, baseClasses: false);
    public static readonly string[] DoHLJobs = GetAbbreviations(JobRole.DoHL, baseClasses: false);
    public static readonly string[] BaseClassJobs = AllDefinitions.Where(d => d.IsBaseClass).Select(d => d.Abbreviation).ToArray();
    public static readonly string[] AllAbbreviations = AllDefinitions.Select(d => d.Abbreviation).ToArray();

    // ── Identity lookups ──

    public static bool TryGet(string key, out JobEntry entry)
    {
        if (!string.IsNullOrEmpty(key) && Lookup.TryGetValue(key, out var def))
        {
            entry = new JobEntry(def.Abbreviation, def.FullName, def.Role, def.ClassJobId, def.DefaultColor, def.IsBaseClass);
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
    public static HashSet<uint> GetKnownReflectStatusIds() => AggregatedKnownReflectStatusIds;
    public static Dictionary<uint, string> GetGroundEffectDotIds() => AggregatedGroundEffectDots;

    // ── Positional fallbacks ──

    public static IEnumerable<PositionalFallbackEntry> GetAllFallbackPositionals() =>
        AllDefinitions.SelectMany(d => d.FallbackPositionals);

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

    private static Dictionary<uint, TValue> BuildAggregatedDict<TValue>(Func<JobDefinitionBase, IReadOnlyDictionary<uint, TValue>> selector)
    {
        var result = new Dictionary<uint, TValue>();
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

    // ── Identity payload returned by lookups ──

    public readonly record struct JobEntry(
        string Abbreviation,
        string FullName,
        JobRole Role,
        uint ClassJobId,
        Vector4 DefaultColor,
        bool IsBaseClass);

    // ── Data-only job (identity properties, no combat/sample data) ──

    private sealed class IdentityJob(
        string abbreviation, string fullName, JobRole role, uint classJobId, Vector4 defaultColor, bool isBaseClass = false)
        : JobDefinitionBase
    {
        public override string Abbreviation => abbreviation;
        public override string FullName => fullName;
        public override JobRole Role => role;
        public override uint ClassJobId => classJobId;
        public override Vector4 DefaultColor => defaultColor;
        public override bool IsBaseClass => isBaseClass;
    }
}
