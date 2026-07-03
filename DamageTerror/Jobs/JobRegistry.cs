
namespace DamageTerror.Jobs;

public static class JobRegistry
{
    // Shared role default for every Disciple of the Hand/Land.
    private static readonly Vector4 DoHLColor = new(0.70f, 0.55f, 0.30f, 1.0f);

    // Opaque colour authored as 8-bit channels (value == channel / 255f).
    private static Vector4 Rgb(byte r, byte g, byte b) => new(r / 255f, g / 255f, b / 255f, 1f);

    private static readonly JobDefinitionBase[] AllDefinitions =
    [
        // ── Tanks ──
        new("Pld", "Paladin", JobRole.Tank, 19, Rgb(168, 210, 230),
            dotTickPotencies: new() { { 248, 30 } },
            dotInitialHitPotencies: new() { { 248, 140 } },
            hotTickPotencies: new() { { 2676, 250 } }),

        new("War", "Warrior", JobRole.Tank, 21, Rgb(207, 38, 33),
            hotTickPotencies: new() { { 2681, 200 }, { 2108, 100 } },
            knownReflectStatusIds: [89, 3832]),

        new("Drk", "Dark Knight", JobRole.Tank, 32, Rgb(209, 38, 204),
            dotTickPotencies: new() { { 749, 50 }, { 3036, 80 } },
            hotTickPotencies: new() { { 3037, 80 } },
            groundEffectDots: new() { { 749, "Salted Earth" }, { 3036, "Salted Earth" } }),

        new("Gnb", "Gunbreaker", JobRole.Tank, 37, Rgb(121, 109, 48),
            dotTickPotencies: new() { { 1837, 120 }, { 1838, 60 } },
            dotInitialHitPotencies: new() { { 1837, 340 }, { 1838, 150 } },
            hotTickPotencies: new() { { 1835, 200 } }),

        // ── Healers ──
        new("Whm", "White Mage", JobRole.Healer, 24, Rgb(255, 240, 220),
            dotTickPotencies: new() { { 1871, 85 }, { 143, 30 }, { 144, 50 }, { 798, 50 } },
            dotInitialHitPotencies: new() { { 1871, 85 } },
            hotTickPotencies: new() { { 158, 250 }, { 150, 150 }, { 3880, 175 }, { 1911, 100 } }),

        new("Sch", "Scholar", JobRole.Healer, 28, Rgb(134, 87, 255),
            dotTickPotencies: new() { { 1895, 85 }, { 189, 40 }, { 3883, 140 }, { 2039, 50 } },
            dotInitialHitPotencies: new() { { 1895, 75 } },
            hotTickPotencies: new() { { 315, 120 }, { 1874, 120 }, { 1944, 100 }, { 3885, 100 } }),

        new("Ast", "Astrologian", JobRole.Healer, 33, Rgb(255, 231, 74),
            dotTickPotencies: new() { { 838, 50 }, { 843, 60 }, { 1881, 70 } },
            hotTickPotencies: new() { { 835, 250 }, { 836, 150 }, { 3894, 175 }, { 848, 100 }, { 956, 100 } },
            groundEffectDots: new() { { 1122, "Earthly Star" } }),

        new("Sge", "Sage", JobRole.Healer, 40, Rgb(128, 160, 240),
            dotTickPotencies: new() { { 2614, 40 }, { 2615, 60 }, { 2616, 90 }, { 3897, 40 }, { 3976, 50 } },
            hotTickPotencies: new() { { 2617, 100 }, { 2620, 100 }, { 2938, 100 }, { 3898, 170 } }),

        // ── Melee DPS ──
        new("Mnk", "Monk", JobRole.MeleeDps, 20, Rgb(214, 156, 0),
            fallbackPositionals:
            [
                new(56, "Snap Punch", "Flank", [(0, false), (16, false), (25, false), (17, true), (27, true), (20, true), (30, true)]),
                new(66, "Demolish", "Rear", [(0, false), (15, true), (18, true)]),
                new(36947, "Pouncing Coeurl", "Flank", [(0, false), (23, false), (15, true), (18, true), (12, true), (14, true)]),
            ]),

        new("Drg", "Dragoon", JobRole.MeleeDps, 22, Rgb(65, 100, 205),
            dotTickPotencies: new() { { 118, 40 }, { 2719, 45 } },
            dotInitialHitPotencies: new() { { 118, 100 }, { 2719, 300 } },
            fallbackPositionals:
            [
                new(3554, "Fang and Claw", "Flank", [(0, false), (53, false), (10, true), (11, true), (58, true), (59, true)]),
                new(3556, "Wheeling Thrust", "Rear", [(0, false), (53, false), (10, true), (11, true), (58, true), (59, true)]),
                new(25772, "Chaotic Spring", "Rear", [(0, false), (53, false), (10, true), (11, true), (58, true), (59, true)]),
            ]),

        new("Nin", "Ninja", JobRole.MeleeDps, 30, Rgb(175, 25, 100),
            dotTickPotencies: new() { { 501, 50 }, { 3184, 80 }, { 4304, 50 } },
            hotTickPotencies: new() { { 3189, 65 } },
            groundEffectDots: new() { { 501, "Doton" }, { 4304, "Doton" } },
            fallbackPositionals:
            [
                new(2255, "Aeolian Edge", "Rear", [(0, false), (47, false), (23, true), (30, true), (56, true), (59, true)]),
                new(2258, "Trick Attack", "Rear", [(0, false), (25, true)]),
                new(3563, "Armor Crush", "Flank", [(0, false), (47, false), (21, true), (27, true), (53, true), (58, true)]),
            ]),

        new("Sam", "Samurai", JobRole.MeleeDps, 34, Rgb(228, 109, 4),
            dotTickPotencies: new() { { 1228, 45 } },
            dotInitialHitPotencies: new() { { 1228, 200 } },
            fallbackPositionals:
            [
                new(7481, "Gekko", "Rear", [(0, false), (53, false), (10, true), (22, true), (11, true), (58, true)]),
                new(7482, "Kasha", "Flank", [(0, false), (53, false), (10, true), (22, true), (11, true), (58, true)]),
            ]),

        new("Rpr", "Reaper", JobRole.MeleeDps, 39, Rgb(150, 90, 144),
            hotTickPotencies: new() { { 2862, 100 } },
            fallbackPositionals:
            [
                new(24382, "Gibbet", "Flank", [(0, false), (10, false), (11, true), (19, true)]),
                new(24383, "Gallows", "Rear", [(0, false), (10, false), (11, true), (19, true)]),
                new(36970, "Executioner's Gibbet", "Flank", [(0, false), (7, true)]),
                new(36971, "Executioner's Gallows", "Rear", [(0, false), (7, true)]),
            ]),

        new("Vpr", "Viper", JobRole.MeleeDps, 41, Rgb(16, 130, 16),
            dotTickPotencies: new() { { 3667, 35 } },
            dotInitialHitPotencies: new() { { 3667, 200 } },
            fallbackPositionals:
            [
                new(34610, "Flanksting Strike", "Flank", [(0, false), (15, true), (12, true)]),
                new(34611, "Flanksbane Fang", "Flank", [(0, false), (15, true), (12, true)]),
                new(34612, "Hindsting Strike", "Rear", [(0, false), (15, true), (12, true)]),
                new(34613, "Hindsbane Fang", "Rear", [(0, false), (15, true), (12, true)]),
                new(34621, "Hunter's Coil", "Rear", [(0, false), (9, true)]),
                new(34622, "Swiftskin's Coil", "Flank", [(0, false), (9, true)]),
            ]),

        // ── Ranged DPS ──
        new("Brd", "Bard", JobRole.RangedDps, 23, Rgb(145, 186, 94),
            dotTickPotencies: new() { { 124, 15 }, { 129, 20 }, { 1200, 20 }, { 1201, 25 } },
            dotInitialHitPotencies: new() { { 1200, 150 }, { 1201, 100 } }),

        new("Mch", "Machinist", JobRole.RangedDps, 31, Rgb(110, 225, 214),
            dotTickPotencies: new() { { 1866, 50 }, { 2019, 65 } },
            dotInitialHitPotencies: new() { { 1866, 50 } }),

        new("Dnc", "Dancer", JobRole.RangedDps, 38, Rgb(226, 176, 175),
            dotTickPotencies: new() { { 3162, 75 } },
            hotTickPotencies: new() { { 2695, 100 } },
            groundEffectDots: new() { { 3162, "Honing Dance" } }),

        // ── Caster DPS ──
        new("Blm", "Black Mage", JobRole.CasterDps, 25, Rgb(165, 121, 214),
            dotTickPotencies: new() { { 163, 50 }, { 1210, 35 }, { 3871, 60 }, { 3872, 40 } },
            dotInitialHitPotencies: new() { { 163, 120 }, { 1210, 80 }, { 3871, 150 }, { 3872, 100 } }),

        new("Smn", "Summoner", JobRole.CasterDps, 27, Rgb(45, 155, 120),
            dotTickPotencies: new() { { 2706, 30 }, { 3231, 65 } },
            groundEffectDots: new() { { 2706, "Slipstream" } }),

        new("Rdm", "Red Mage", JobRole.CasterDps, 35, Rgb(232, 123, 123),
            dotTickPotencies: new() { { 4319, 65 } }),

        new("Pct", "Pictomancer", JobRole.CasterDps, 42, Rgb(252, 146, 225)),

        new("Blu", "Blue Mage", JobRole.CasterDps, 36, new(0.30f, 0.55f, 0.90f, 1.0f),
            dotTickPotencies: new() { { 1714, 50 }, { 1736, 50 }, { 18, 30 }, { 1723, 20 }, { 3712, 80 }, { 3643, 50 } },
            hotTickPotencies: new() { { 2495, 100 } },
            knownReflectStatusIds: [1720, 1724, 3631]),

        // ── Base Classes ──
        new("Gla", "Gladiator", JobRole.Tank, 1, Rgb(168, 210, 230), isBaseClass: true),
        new("Mrd", "Marauder", JobRole.Tank, 3, Rgb(207, 38, 33), isBaseClass: true),
        new("Cnj", "Conjurer", JobRole.Healer, 6, Rgb(255, 240, 220), isBaseClass: true),
        new("Pgl", "Pugilist", JobRole.MeleeDps, 2, Rgb(214, 156, 0), isBaseClass: true),
        new("Lnc", "Lancer", JobRole.MeleeDps, 4, Rgb(65, 100, 205), isBaseClass: true),
        new("Arc", "Archer", JobRole.RangedDps, 5, Rgb(145, 186, 94), isBaseClass: true),
        new("Rog", "Rogue", JobRole.MeleeDps, 29, Rgb(175, 25, 100), isBaseClass: true),
        new("Thm", "Thaumaturge", JobRole.CasterDps, 7, Rgb(165, 121, 214), isBaseClass: true),
        new("Acn", "Arcanist", JobRole.CasterDps, 26, Rgb(45, 155, 120), isBaseClass: true),

        // ── Crafters ──
        new("Crp", "Carpenter", JobRole.DoHL, 8, DoHLColor),
        new("Bsm", "Blacksmith", JobRole.DoHL, 9, DoHLColor),
        new("Arm", "Armorer", JobRole.DoHL, 10, DoHLColor),
        new("Gsm", "Goldsmith", JobRole.DoHL, 11, DoHLColor),
        new("Ltw", "Leatherworker", JobRole.DoHL, 12, DoHLColor),
        new("Wvr", "Weaver", JobRole.DoHL, 13, DoHLColor),
        new("Alc", "Alchemist", JobRole.DoHL, 14, DoHLColor),
        new("Cul", "Culinarian", JobRole.DoHL, 15, DoHLColor),

        // ── Gatherers ──
        new("Min", "Miner", JobRole.DoHL, 16, DoHLColor),
        new("Btn", "Botanist", JobRole.DoHL, 17, DoHLColor),
        new("Fsh", "Fisher", JobRole.DoHL, 18, DoHLColor),

        // ── Special ──
        new("Lmb", "Limit Break", JobRole.LimitBreak, 0, new(0.5f, 0.5f, 0.5f, 1.0f)),
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
}
