using Dalamud.Bindings.ImGui;
using DamageTerror.Jobs;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

public sealed class SampleDataPage
{
    private readonly DamageTerrorPlugin plugin;

    private static readonly string[] PresetNames =
    {
        "8-Player Raid (Full Party)",
        "4-Player Dungeon",
        "24-Player Alliance Raid",
        "72-Player PvP (Frontline)",
        "200-Player Hunt Train",
        "9999-Player Stress Test",
    };

    private int selectedPreset;
    private bool simulateCombat;

    public SampleDataPage(DamageTerrorPlugin plugin)
    {
        this.plugin = plugin;
    }

    public void Draw()
    {
        var store = plugin.DataService.Store;
        var sampleLoaded = store.IsSampleDataActive;

        ImGui.TextUnformatted("Sample Data");
        ConfigHelpers.HelpMarker("Load a simulated encounter to preview and test your UI settings.\nSample data is temporary and will not be saved to history.");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.SetNextItemWidth(250);
        ImGui.Combo("Preset", ref selectedPreset, PresetNames, PresetNames.Length);
        ImGui.Spacing();

        if (ImGui.Checkbox("Simulate active combat", ref simulateCombat))
        {
            if (sampleLoaded)
                store.SetSampleSimulation(simulateCombat);
        }
        ConfigHelpers.HelpMarker("When enabled, numbers will fluctuate in real-time like a live encounter.");
        ImGui.Spacing();

        if (ImGui.Button("Load Sample Encounter", new Vector2(220, 0)))
        {
            LoadSampleEncounter(selectedPreset);
        }

        if (sampleLoaded)
        {
            ImGui.SameLine();
            if (ImGui.Button("Clear Sample Data", new Vector2(180, 0)))
            {
                ClearSampleData();
            }
        }

        ImGui.Spacing();

        if (sampleLoaded)
        {
            var active = store.ActiveEncounter;
            var count = active?.Combatants.Count ?? 0;
            ImGui.TextColored(new Vector4(0.4f, 1.0f, 0.4f, 1.0f),
                $"Sample encounter loaded ({count} players). Enable simulation and check the main meter window.");
        }
    }

    private void LoadSampleEncounter(int preset)
    {
        var store = plugin.DataService.Store;

        if (preset == 5)
        {
            var (snapshot, factory) = SampleDataGenerator.CreateStressTest();
            store.LoadSampleData(snapshot, simulate: simulateCombat, combatantFactory: factory);
            return;
        }

        var regular = preset switch
        {
            1 => SampleDataGenerator.CreateDungeonParty(),
            2 => SampleDataGenerator.CreateAllianceRaid(),
            3 => SampleDataGenerator.CreateFrontline(),
            4 => SampleDataGenerator.CreateHuntTrain(),
            _ => SampleDataGenerator.CreateFullParty(),
        };

        store.LoadSampleData(regular, simulate: simulateCombat);
    }

    private void ClearSampleData()
    {
        plugin.DataService.Store.ClearSampleData();
    }
}

internal static class SampleDataGenerator
{
    private static readonly Random Rng = new();

    public static EncounterSnapshot CreateFullParty()
    {
        var combatants = new List<CombatantEntry>
        {
            MakeCombatant("Vermillion Terror", "Blm", 28200, 0, isLocal: true),
            MakeCombatant("Rtb Baytolachefe", "Pld", 14500, 0),
            MakeCombatant("Marcelo Benevides", "Whm", 8000, 18500),
            MakeCombatant("Red Diamond", "Drg", 26500, 0),
            MakeCombatant("Atrina Vermillion", "Rpr", 29200, 0),
            MakeCombatant("Nikita Airisu", "Sge", 7800, 0),
            MakeCombatant("Kotoshiro Dazaria", "War", 15600, 0),
            MakeCombatant("Nestfexia Reanna", "Rdm", 30100, 4200),
        };

        return BuildSnapshot(combatants, "The Omega Protocol (Ultimate)", "Alphascape V4.0", "08:32");
    }

    public static EncounterSnapshot CreateDungeonParty()
    {
        var combatants = new List<CombatantEntry>
        {
            MakeCombatant("Krile Baldesion", "Sge", 3600, 17200, isLocal: true),
            MakeCombatant("Y'shtola Rhul", "Blm", 24800, 0),
            MakeCombatant("Estinien Wyrmblood", "Drg", 23100, 0),
            MakeCombatant("Haurchefant Greystone", "Pld", 14200, 0),
        };

        return BuildSnapshot(combatants, "The Aetherfont", "The Aetherfont", "04:15");
    }

    public static EncounterSnapshot CreateAllianceRaid()
    {
        var combatants = new List<CombatantEntry>();
        var names = new[]
        {
            ("Kotoshiro Dazaria", "War"), ("Onyx Shield", "Drk"),
            ("Nestefxia Reanna", "Rdm"), ("Shadow Strike", "Nin"),
            ("Wild Rose", "Brd"), ("Guiding Light", "Sge"),
            ("Thunder Fist", "Mnk"), ("Frost Caller", "Blm"),
            ("Cerulean Shot", "Mch"), ("Marcelo Benevides", "Whm"),
            ("Red Diamond", "Drg"), ("Nikita Airisu", "Pct"),
            ("Blazing Heart", "Sam"), ("Atrina Vermillion", "Rpr"),
            ("Dusk Warden", "Gnb"), ("Morning Dew", "Ast"),
            ("Rtb Baytolachefe", "Pld"), ("Tidal Wave", "Smn"),
            ("Gentle Spark", "Dnc"), ("Arctic Fox", "Vpr"),
            ("Crimson Tide", "War"), ("Night Bloom", "Sch"),
            ("Solar Flare", "Blm"), ("Verdant Vine", "Whm"),
        };

        var isFirst = true;
        foreach (var (name, job) in names)
        {
            var isHealer = job is "Whm" or "Sch" or "Ast" or "Sge";
            var dps = isHealer ? Rng.Next(2800, 5000) : Rng.Next(14000, 29000);
            var hps = isHealer ? Rng.Next(14000, 19000) : 0;
            combatants.Add(MakeCombatant(name, job, dps, hps, isLocal: isFirst));
            isFirst = false;
        }

        return BuildSnapshot(combatants, "The Cloud of Darkness", "The World of Darkness", "12:45");
    }

    public static EncounterSnapshot CreateFrontline()
    {
        var combatants = new List<CombatantEntry>();

        var pvpNames = new[]
        {
            // Maelstrom (24)
            ("Kotoshiro Dazaria", "War"), ("Rtb Baytolachefe", "Pld"), ("Gale Runner", "Nin"), ("Vermillion Terrorr", "Blm"),
            ("Coral Shield", "Gnb"), ("Marcelo Benevides", "Whm"), ("Red Diamond", "Drg"), ("Riptide Shot", "Mch"),
            ("Sea Breeze", "Dnc"), ("Ocean Fury", "Sam"), ("Atrina Vermillion", "Rpr"), ("Brine Sage", "Sge"),
            ("Sanaya Minatozaki", "Drg"), ("Whirlpool", "Smn"), ("Tsunami Edge", "Drk"), ("Tide Caller", "Ast"),
            ("Surge Strike", "Vpr"), ("Nikita Airisu", "Pct"), ("Tataru Terror", "Rdm"), ("Nestefxia Reanna", "Rdm"),
            ("Salt Spray", "War"), ("Abyssal Ward", "Sch"), ("Storm Chaser", "Blm"), ("Oleg Arkwirit", "Pld"),

            // Twin Adder (24)
            ("Verdant Blade", "Pld"), ("Thornwall", "Drk"), ("Ivy Strike", "Nin"), ("Petal Storm", "Dnc"),
            ("Root Warden", "Gnb"), ("Bloom Healer", "Whm"), ("Leaf Dancer", "Drg"), ("Acorn Shot", "Mch"),
            ("Blossom Rain", "Brd"), ("Oak Fist", "Mnk"), ("Vine Reaper", "Rpr"), ("Moss Sage", "Sge"),
            ("Forest Fury", "Sam"), ("Sprout Mage", "Smn"), ("Bark Shield", "War"), ("Dew Drop", "Ast"),
            ("Fern Strike", "Vpr"), ("Pollen Burst", "Pct"), ("Canopy Shade", "Blm"), ("Willow Grace", "Rdm"),
            ("Green Tide", "Drk"), ("Meadow Song", "Sch"), ("Thicket Guard", "Pld"), ("Briar Thorn", "Gnb"),

            // Immortal Flames (24)
            ("Inferno Blade", "War"), ("Ash Shield", "Pld"), ("Ember Strike", "Nin"), ("Blaze Caster", "Blm"),
            ("Cinder Guard", "Gnb"), ("Flame Healer", "Whm"), ("Scorch Lance", "Drg"), ("Flint Shot", "Mch"),
            ("Spark Dancer", "Dnc"), ("Pyre Fist", "Mnk"), ("Char Reaper", "Rpr"), ("Soot Sage", "Sge"),
            ("Magma Edge", "Sam"), ("Sulfur Mage", "Smn"), ("Obsidian Wall", "Drk"), ("Warmth", "Ast"),
            ("Viper Flame", "Vpr"), ("Fire Bloom", "Pct"), ("Coal Arrow", "Brd"), ("Crimson Spell", "Rdm"),
            ("Slag Guard", "War"), ("Torch Song", "Sch"), ("Ignis Star", "Blm"), ("Basalt Lord", "Gnb"),
        };

        var isFirst = true;
        foreach (var (name, job) in pvpNames)
        {
            var isHealer = job is "Whm" or "Sch" or "Ast" or "Sge";
            var isTank = job is "War" or "Pld" or "Drk" or "Gnb";
            var dps = isHealer ? Rng.Next(1800, 4000) : isTank ? Rng.Next(3000, 7000) : Rng.Next(5000, 14000);
            var hps = isHealer ? Rng.Next(8000, 16000) : 0;
            combatants.Add(MakeCombatant(name, job, dps, hps, isLocal: isFirst));
            isFirst = false;
        }

        return BuildSnapshot(combatants, "Frontline: Onsal Hakair", "Onsal Hakair", "20:00");
    }

    public static EncounterSnapshot CreateHuntTrain()
    {
        var combatants = new List<CombatantEntry>();
        var allJobs = new[] { "War", "Pld", "Drk", "Gnb", "Whm", "Sch", "Ast", "Sge", "Mnk", "Drg", "Nin", "Sam", "Rpr", "Vpr", "Brd", "Mch", "Dnc", "Blm", "Smn", "Rdm", "Pct" };

        var firstNames = new[]
        {
            "Azure", "Crimson", "Golden", "Silver", "Iron", "Storm", "Frost", "Shadow", "Dawn",
            "Dusk", "Onyx", "Ruby", "Jade", "Pearl", "Amber", "Coral", "Ivory", "Scarlet",
            "Violet", "Cobalt", "Bronze", "Marble", "Obsidian", "Copper", "Crystal", "Flint",
            "Garnet", "Hazel", "Indigo", "Jasper", "Lapis", "Malachite", "Opal", "Quartz",
            "Slate", "Terra", "Umber", "Vermil", "Willow", "Zephyr",
        };
        var lastNames = new[]
        {
            "Blade", "Shield", "Arrow", "Fist", "Song", "Light", "Star", "Moon", "Sun",
            "Wolf", "Hawk", "Fox", "Bear", "Stag", "Raven", "Lion", "Drake", "Wren",
            "Hare", "Lynx", "Crane", "Viper", "Owl", "Hart", "Finch",
        };

        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < 200; i++)
        {
            string name;
            do
            {
                var first = firstNames[Rng.Next(firstNames.Length)];
                var last = lastNames[Rng.Next(lastNames.Length)];
                name = $"{first} {last}";
            } while (!usedNames.Add(name));

            var job = allJobs[Rng.Next(allJobs.Length)];
            var isHealer = job is "Whm" or "Sch" or "Ast" or "Sge";
            var isTank = job is "War" or "Pld" or "Drk" or "Gnb";
            var dps = isHealer ? Rng.Next(2000, 5000) : isTank ? Rng.Next(8000, 16000) : Rng.Next(14000, 32000);
            var hps = isHealer ? Rng.Next(6000, 14000) : 0;
            combatants.Add(MakeCombatant(name, job, dps, hps, isLocal: i == 0));
        }

        return BuildSnapshot(combatants, "Stolas (S Rank)", "Urqopacha", "01:45");
    }

    public static (EncounterSnapshot Snapshot, Func<CombatantEntry?> Factory) CreateStressTest()
    {
        var allJobs = new[] { "War", "Pld", "Drk", "Gnb", "Whm", "Sch", "Ast", "Sge", "Mnk", "Drg", "Nin", "Sam", "Rpr", "Vpr", "Brd", "Mch", "Dnc", "Blm", "Smn", "Rdm", "Pct" };

        var firstNames = new[]
        {
            "Azure", "Crimson", "Golden", "Silver", "Iron", "Storm", "Frost", "Shadow", "Dawn",
            "Dusk", "Onyx", "Ruby", "Jade", "Pearl", "Amber", "Coral", "Ivory", "Scarlet",
            "Violet", "Cobalt", "Bronze", "Marble", "Obsidian", "Copper", "Crystal", "Flint",
            "Garnet", "Hazel", "Indigo", "Jasper", "Lapis", "Malachite", "Opal", "Quartz",
            "Slate", "Terra", "Umber", "Vermil", "Willow", "Zephyr", "Basalt", "Cedar",
            "Dune", "Echo", "Fenrir", "Gale", "Harbor", "Isle", "Jolt", "Kite",
            "Loom", "Mist", "Neon", "Ore", "Prism", "Reef", "Shard", "Thorn",
            "Umbra", "Vale", "Wane", "Xenon", "Yield", "Zinc", "Agate", "Birch",
            "Cliff", "Drift", "Ember", "Forge", "Glint", "Husk", "Iota", "Jetty",
            "Knoll", "Ledge", "Mesa", "Nexus", "Orbit", "Pulse", "Quirk", "Ridge",
            "Spire", "Torch", "Unity", "Vapor", "Whirl", "Axiom", "Brink", "Crest",
            "Delta", "Epoch", "Flux", "Grove", "Helix", "Index", "Joust", "Karma",
        };
        var lastNames = new[]
        {
            "Blade", "Shield", "Arrow", "Fist", "Song", "Light", "Star", "Moon", "Sun",
            "Wolf", "Hawk", "Fox", "Bear", "Stag", "Raven", "Lion", "Drake", "Wren",
            "Hare", "Lynx", "Crane", "Viper", "Owl", "Hart", "Finch", "Flame",
            "Thorn", "Frost", "Storm", "Tide", "Wind", "Stone", "Brook", "Peak",
            "Root", "Leaf", "Bark", "Bloom", "Dew", "Glen", "Marsh", "Reed",
            "Sand", "Vale", "Cove", "Ridge", "Dell", "Knoll", "Bluff", "Gorge",
            "Hollow", "Ledge", "Shoal", "Forge", "Smith", "Ward", "Guard", "March",
            "Born", "Fall", "Rise", "Dawn", "Dusk", "Night", "Noon", "Rain",
            "Snow", "Hail", "Gust", "Bolt", "Ember", "Ash", "Soot", "Coal",
            "Rust", "Moss", "Vine", "Fern", "Ivy", "Rose", "Sage", "Wort",
            "Bane", "Boon", "Gift", "Oath", "Pact", "Vow", "Rite", "Rune",
            "Spell", "Hex", "Charm", "Grace", "Hope", "Fate", "Soul", "Heart",
        };

        // Create the first combatant immediately
        var first = MakeCombatant($"{firstNames[0]} {lastNames[0]}", allJobs[0], 15000, 0, isLocal: true);
        var initial = new List<CombatantEntry> { first };
        var snapshot = BuildSnapshot(initial, "Stress Test (9999 Players)", "Mor Dhona", "05:00");

        // Lazy factory generates one combatant per call, no upfront cost
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { first.Name };
        var generated = 1;
        const int maxCount = 9999;

        Func<CombatantEntry?> factory = () =>
        {
            if (generated >= maxCount) return null;

            string name;
            var attempts = 0;
            do
            {
                var fn = firstNames[Rng.Next(firstNames.Length)];
                var ln = lastNames[Rng.Next(lastNames.Length)];
                name = $"{fn} {ln}";
                attempts++;
                if (attempts > 20)
                {
                    // Append number suffix to guarantee uniqueness past pool exhaustion
                    name = $"{fn} {ln}{generated}";
                }
            } while (!usedNames.Add(name));

            generated++;
            var job = allJobs[Rng.Next(allJobs.Length)];
            var isHealer = job is "Whm" or "Sch" or "Ast" or "Sge";
            var isTank = job is "War" or "Pld" or "Drk" or "Gnb";
            var dps = isHealer ? Rng.Next(2000, 5000) : isTank ? Rng.Next(8000, 16000) : Rng.Next(14000, 32000);
            var hps = isHealer ? Rng.Next(6000, 14000) : 0;
            return MakeCombatant(name, job, dps, hps);
        };

        return (snapshot, factory);
    }

    private static EncounterSnapshot BuildSnapshot(
        List<CombatantEntry> combatants, string title, string zone, string duration)
    {
        var durationSec = DurationHelper.ParseDuration(duration, 60f);
        long totalDamage = 0;
        long totalHealed = 0;
        int totalDeaths = 0;

        foreach (var c in combatants)
        {
            c.Damage = (long)(c.EncDps * durationSec);
            c.Healed = (long)(c.EncHps * durationSec);
            totalDamage += c.Damage;
            totalHealed += c.Healed;
            totalDeaths += c.Deaths;
        }

        var totalDps = durationSec > 0 ? totalDamage / durationSec : 0;
        var totalHps = durationSec > 0 ? totalHealed / durationSec : 0;

        foreach (var c in combatants)
        {
            c.DamagePercent = totalDamage > 0
                ? $"{(double)c.Damage / totalDamage * 100:F1}%"
                : "0%";
            c.HealedPercent = totalHealed > 0
                ? $"{(double)c.Healed / totalHealed * 100:F1}%"
                : "0%";
        }

        var snapshot = new EncounterSnapshot
        {
            Encounter = new CombatEncounter
            {
                Title = title,
                Duration = duration,
                ZoneName = zone,
                EncDps = totalDps,
                EncHps = totalHps,
                TotalDamage = totalDamage,
                TotalHealed = totalHealed,
                Deaths = totalDeaths,
                IsActive = false,
            },
            Combatants = combatants,
            Timestamp = DateTime.UtcNow,
            PlayerName = combatants.FirstOrDefault(c => c.IsLocalPlayer)?.Name ?? string.Empty,
        };

        var graphData = new Dictionary<string, List<GraphSample>>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in combatants)
        {
            var samples = GenerateGraphSamples(c, durationSec);
            graphData[c.Name] = samples;
        }
        snapshot.GraphData = graphData;

        var statusHistory = new Dictionary<string, List<StatusApplication>>(StringComparer.OrdinalIgnoreCase);
        var statusesReceived = new Dictionary<string, List<StatusApplication>>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in combatants)
        {
            var applied = GenerateAppliedStatuses(c, combatants, durationSec);
            if (applied.Count > 0)
                statusHistory[c.Name] = applied;
        }

        // Build received statuses by inverting applied
        foreach (var (source, apps) in statusHistory)
        {
            foreach (var s in apps)
            {
                if (!statusesReceived.TryGetValue(s.TargetName, out var list))
                {
                    list = new List<StatusApplication>();
                    statusesReceived[s.TargetName] = list;
                }
                list.Add(s);
            }
        }

        snapshot.StatusHistory = statusHistory;
        snapshot.StatusesReceived = statusesReceived;

        return snapshot;
    }

    private static CombatantEntry MakeCombatant(
        string name, string job, double dps, double hps, bool isLocal = false)
    {
        var critPct = 15.0 + Rng.NextDouble() * 20.0;
        var dhPct = 20.0 + Rng.NextDouble() * 20.0;
        var cdhPct = Math.Min(critPct, dhPct) * 0.4 + Rng.NextDouble() * 5.0;
        var deaths = Rng.NextDouble() < 0.15 ? Rng.Next(1, 3) : 0;
        var overhealPct = hps > 0 ? 10.0 + Rng.NextDouble() * 30.0 : 0;

        var entry = new CombatantEntry
        {
            Name = name,
            Job = job,
            EncDps = dps,
            EncHps = hps,
            CritPct = Math.Round(critPct, 1),
            DirectHitPct = Math.Round(dhPct, 1),
            CritDirectHitPct = Math.Round(cdhPct, 1),
            Deaths = deaths,
            DamageTaken = Rng.Next(50000, 250000),
            OverhealPct = Math.Round(overhealPct, 1),
            MaxHit = GenerateMaxHit(job),
            MaxHitDamage = Rng.Next(30000, 120000),
            IsLocalPlayer = isLocal,
            PeakDps = dps * (1.1 + Rng.NextDouble() * 0.3),
            Swings = Rng.Next(200, 600),
            Hits = Rng.Next(180, 580),
            CritHitCount = Rng.Next(40, 120),
            DirectHitCount = Rng.Next(50, 150),
            CritDirectHitCount = Rng.Next(10, 40),
            InstantDps = dps * (0.85 + Rng.NextDouble() * 0.3),
            InstantHps = hps * (0.85 + Rng.NextDouble() * 0.3),
        };

        entry.HitRate = entry.Swings > 0 ? (double)entry.Hits / entry.Swings * 100.0 : 0;
        entry.Misses = entry.Swings - entry.Hits;
        entry.DamageTakenPercent = $"{Rng.Next(8, 16)}%";

        entry.Skills = GenerateSkills(job, isDamage: true);
        entry.HealingSkills = hps > 0 ? GenerateSkills(job, isDamage: false) : new();

        return entry;
    }

    private static string GenerateMaxHit(string job)
    {
        var skills = JobRegistry.GetMaxHitSkill(job);
        return $"{skills}-{Rng.Next(30000, 120000)}";
    }

    private static List<SkillEntry> GenerateSkills(string job, bool isDamage)
    {
        var skills = new List<SkillEntry>();
        var skillNames = isDamage
            ? GetDamageSkillNames(job)
            : GetHealSkillNames(job);

        var totalWeight = 0.0;
        var weights = new double[skillNames.Length];
        for (var i = 0; i < skillNames.Length; i++)
        {
            weights[i] = 1.0 / (i + 1) + Rng.NextDouble() * 0.3;
            totalWeight += weights[i];
        }

        for (var i = 0; i < skillNames.Length; i++)
        {
            var pct = weights[i] / totalWeight * 100.0;
            skills.Add(new SkillEntry
            {
                Name = skillNames[i],
                TotalDamage = (long)(Rng.Next(50000, 500000) * (weights[i] / totalWeight)),
                HitCount = Rng.Next(5, 80),
                DamagePercent = Math.Round(pct, 1),
                CritPct = Math.Round(15.0 + Rng.NextDouble() * 25.0, 1),
                DirectHitPct = Math.Round(20.0 + Rng.NextDouble() * 20.0, 1),
                CritDirectHitPct = Math.Round(5.0 + Rng.NextDouble() * 10.0, 1),
                DamageType = isDamage
                    ? (Rng.NextDouble() > 0.5 ? SkillDamageType.Physical : SkillDamageType.Magic)
                    : SkillDamageType.Magic,
            });
        }

        return skills;
    }

    private static string[] GetDamageSkillNames(string job) => JobRegistry.GetDamageSkillNames(job);

    private static string[] GetHealSkillNames(string job) => JobRegistry.GetHealSkillNames(job);

    private static List<GraphSample> GenerateGraphSamples(CombatantEntry c, float durationSec)
    {
        var samples = new List<GraphSample>();
        var interval = 0.5f;
        var baseDps = (float)c.EncDps;
        var baseHps = (float)c.EncHps;

        for (var t = 0f; t <= durationSec; t += interval)
        {
            var variance = 0.7f + (float)Rng.NextDouble() * 0.6f;
            var hVariance = 0.5f + (float)Rng.NextDouble() * 1.0f;

            samples.Add(new GraphSample
            {
                TimeSec = t,
                Dps = baseDps * variance,
                Hps = baseHps * hVariance,
                Dtps = (float)(Rng.NextDouble() * 2000),
            });
        }

        return samples;
    }

    private static List<StatusApplication> GenerateAppliedStatuses(
        CombatantEntry source, List<CombatantEntry> allCombatants, float durationSec)
    {
        var result = new List<StatusApplication>();
        var buffs = GetJobBuffs(source.Job);
        var debuffs = GetJobDebuffs(source.Job);

        // Buffs applied to party members
        foreach (var (id, name, dur, isHot) in buffs)
        {
            var targets = isHot
                ? allCombatants.Where(c => c.EncHps > 0 || Rng.NextDouble() < 0.6).ToList()
                : allCombatants;

            foreach (var target in targets)
            {
                var t = 0f;
                while (t < durationSec)
                {
                    var gap = (float)(Rng.NextDouble() * dur * 0.3);
                    t += gap;
                    if (t >= durationSec) break;

                    var actualDur = Math.Min(dur + (float)(Rng.NextDouble() * 2 - 1), durationSec - t);
                    result.Add(new StatusApplication
                    {
                        StatusId = id,
                        StatusName = name,
                        SourceName = source.Name,
                        TargetName = target.Name,
                        AppliedAtSec = t,
                        Duration = actualDur,
                        RemovedAtSec = t + actualDur,
                        IsBuff = true,
                        IsHoT = isHot,
                    });
                    t += actualDur;
                }
            }
        }

        // Debuffs applied to boss/target
        foreach (var (id, name, dur, isDot) in debuffs)
        {
            var t = 0f;
            while (t < durationSec)
            {
                var gap = (float)(Rng.NextDouble() * dur * 0.2);
                t += gap;
                if (t >= durationSec) break;

                var actualDur = Math.Min(dur + (float)(Rng.NextDouble() * 2 - 1), durationSec - t);
                result.Add(new StatusApplication
                {
                    StatusId = id,
                    StatusName = name,
                    SourceName = source.Name,
                    TargetName = source.Name,
                    AppliedAtSec = t,
                    Duration = actualDur,
                    RemovedAtSec = t + actualDur,
                    IsBuff = false,
                    IsDoT = isDot,
                });
                t += actualDur;
            }
        }

        return result;
    }

    internal static (uint, string, float, bool)[] GetJobBuffs(string job) => JobRegistry.GetJobBuffs(job);

    internal static (uint, string, float, bool)[] GetJobDebuffs(string job) => JobRegistry.GetJobDebuffs(job);
}
