using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

public class SampleDataPage
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
                $"Sample encounter loaded ({count} players). Check the main meter window.");
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
            MakeCombatant("Marcelo Benevides", "Whm", 4800, 18500),
            MakeCombatant("Red Diamond", "Drg", 26500, 0),
            MakeCombatant("Atrina Vermillion", "Rpr", 29200, 0),
            MakeCombatant("Nikita Airisu", "Pct", 27400, 0),
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

        // Compute damage/heal percentages
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

        // Generate graph data
        var graphData = new Dictionary<string, List<GraphSample>>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in combatants)
        {
            var samples = GenerateGraphSamples(c, durationSec);
            graphData[c.Name] = samples;
        }
        snapshot.GraphData = graphData;

        // Generate buff/debuff status data
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

        // Generate skill breakdowns
        entry.Skills = GenerateSkills(job, isDamage: true);
        entry.HealingSkills = hps > 0 ? GenerateSkills(job, isDamage: false) : new();

        return entry;
    }

    private static string GenerateMaxHit(string job)
    {
        var skills = job switch
        {
            "Sam" => "Midare Setsugekka",
            "Drg" => "Stardiver",
            "Blm" => "Flare Star",
            "Gnb" => "Blasting Zone",
            "Rpr" => "Communio",
            "Dnc" => "Technical Finish",
            "Whm" => "Glare III",
            "Sch" => "Broil IV",
            "Pld" => "Confiteor",
            "Mnk" => "Phantom Rush",
            "Smn" => "Akh Morn",
            "Ast" => "Fall Malefic",
            "Nin" => "Hyosho Ranryu",
            "Brd" => "Radiant Finale",
            "Mch" => "Wildfire",
            "Rdm" => "Scorch",
            "Pct" => "Star Prism",
            "Vpr" => "Ouroboros",
            "War" => "Primal Rend",
            "Drk" => "Living Shadow",
            "Sge" => "Pneuma",
            _ => "Attack",
        };
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

    private static string[] GetDamageSkillNames(string job) => job switch
    {
        "Sam" => new[] { "Midare Setsugekka", "Ogi Namikiri", "Kaeshi: Namikiri", "Higanbana", "Shinten", "Shoha" },
        "Drg" => new[] { "Stardiver", "Nastrond", "Heaven's Thrust", "Chaotic Spring", "Wyrmwind Thrust", "Dragonfire Dive" },
        "Blm" => new[] { "Flare Star", "Despair", "Xenoglossy", "Fire IV", "Paradox", "Thunder III" },
        "Gnb" => new[] { "Blasting Zone", "Double Down", "Sonic Break", "Burst Strike", "Gnashing Fang", "Hypervelocity" },
        "Rpr" => new[] { "Communio", "Plentiful Harvest", "Gibbet", "Gallows", "Void Reaping", "Cross Reaping" },
        "Dnc" => new[] { "Technical Finish", "Starfall Dance", "Saber Dance", "Tillana", "Standard Finish", "Fan Dance IV" },
        "Pld" => new[] { "Confiteor", "Blade of Honor", "Holy Spirit", "Atonement", "Goring Blade", "Royal Authority" },
        "Mnk" => new[] { "Phantom Rush", "Elixir Burst", "Rising Phoenix", "Bootshine", "Dragon Kick", "Demolish" },
        "Smn" => new[] { "Akh Morn", "Enkindle Bahamut", "Astral Impulse", "Ruby Rite", "Topaz Rite", "Emerald Rite" },
        "Nin" => new[] { "Hyosho Ranryu", "Forked Raijin", "Fleeting Raijin", "Bhavacakra", "Aeolian Edge", "Armor Crush" },
        "Brd" => new[] { "Radiant Finale", "Blast Arrow", "Apex Arrow", "Refulgent Arrow", "Burst Shot", "Iron Jaws" },
        "Mch" => new[] { "Wildfire", "Chain Saw", "Excavator", "Air Anchor", "Drill", "Heat Blast" },
        "Rdm" => new[] { "Scorch", "Resolution", "Verholy", "Verflare", "Fleche", "Contre Sixte" },
        "Pct" => new[] { "Star Prism", "Comet in Black", "Holy in White", "Fire in Red", "Creature Motif", "Hammer Stamp" },
        "Vpr" => new[] { "Ouroboros", "Reawaken", "Uncoiled Fury", "Hindsting Strike", "Flanksbane Fang", "Hunter's Sting" },
        "War" => new[] { "Primal Rend", "Inner Chaos", "Fell Cleave", "Upheaval", "Onslaught", "Storm's Eye" },
        "Drk" => new[] { "Living Shadow", "Shadowbringer", "Edge of Shadow", "Bloodspiller", "Carve and Spit", "Souleater" },
        "Whm" => new[] { "Glare III", "Afflatus Misery", "Dia", "Assize", "Holy III" },
        "Sch" => new[] { "Broil IV", "Biolysis", "Energy Drain", "Chain Stratagem", "Art of War II" },
        "Ast" => new[] { "Fall Malefic", "Combust III", "Lord of Crowns", "Earthly Star", "Gravity II" },
        "Sge" => new[] { "Dosis III", "Eukrasian Dosis III", "Phlegma III", "Toxikon II", "Pneuma" },
        _ => new[] { "Attack", "Auto-Attack" },
    };

    private static string[] GetHealSkillNames(string job) => job switch
    {
        "Whm" => new[] { "Medica II", "Afflatus Rapture", "Afflatus Solace", "Cure III", "Regen", "Liturgy of the Bell" },
        "Sch" => new[] { "Adloquium", "Succor", "Lustrate", "Excogitation", "Sacred Soil", "Seraphic Veil" },
        "Ast" => new[] { "Aspected Benefic", "Aspected Helios", "Celestial Opposition", "Earthly Star", "Essential Dignity", "Macrocosmos" },
        "Sge" => new[] { "Eukrasian Diagnosis", "Eukrasian Prognosis", "Druochole", "Kerachole", "Ixochole", "Pneuma" },
        _ => new[] { "Second Wind", "Bloodbath" },
    };

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

    // Returns (StatusId, Name, Duration, IsHoT)
    internal static (uint, string, float, bool)[] GetJobBuffs(string job) => job switch
    {
        "War" => new[] { (1001u, "Vengeance", 15f, false), (1002u, "Thrill of Battle", 10f, false), (1003u, "Shake It Off", 30f, false) },
        "Pld" => new[] { (1010u, "Sentinel", 15f, false), (1011u, "Divine Veil", 30f, false), (1012u, "Hallowed Ground", 10f, false) },
        "Drk" => new[] { (1020u, "Shadow Wall", 15f, false), (1021u, "Dark Mind", 10f, false), (1022u, "The Blackest Night", 7f, false) },
        "Gnb" => new[] { (1030u, "Nebula", 15f, false), (1031u, "Camouflage", 20f, false), (1032u, "Heart of Corundum", 8f, false) },
        "Whm" => new[] { (1100u, "Regen", 18f, true), (1101u, "Medica II", 15f, true), (1102u, "Temperance", 22f, false), (1103u, "Liturgy of the Bell", 20f, false) },
        "Sch" => new[] { (1110u, "Galvanize", 30f, false), (1111u, "Sacred Soil", 15f, true), (1112u, "Expedient", 20f, false), (1113u, "Seraphic Veil", 30f, false) },
        "Ast" => new[] { (1120u, "Aspected Benefic", 15f, true), (1121u, "Aspected Helios", 15f, true), (1122u, "Earthly Star", 20f, false), (1123u, "The Arrow", 15f, false), (1124u, "The Balance", 15f, false) },
        "Sge" => new[] { (1130u, "Eukrasian Diagnosis", 30f, false), (1131u, "Kerachole", 15f, true), (1132u, "Holos", 20f, false), (1133u, "Physis II", 15f, true) },
        "Mnk" => new[] { (1200u, "Brotherhood", 20f, false), (1201u, "Mantra", 15f, false) },
        "Drg" => new[] { (1210u, "Battle Litany", 20f, false), (1211u, "Dragon Sight", 20f, false) },
        "Nin" => new[] { (1220u, "Trick Attack", 15f, false) },
        "Sam" => new[] { (1230u, "Meikyo Shisui", 15f, false) },
        "Rpr" => new[] { (1240u, "Arcane Circle", 20f, false) },
        "Vpr" => new[] { (1250u, "Serpent's Ire", 15f, false) },
        "Brd" => new[] { (1300u, "Mage's Ballad", 45f, false), (1301u, "Army's Paeon", 45f, false), (1302u, "The Wanderer's Minuet", 45f, false), (1303u, "Radiant Finale", 20f, false) },
        "Mch" => new[] { (1310u, "Reassemble", 5f, false) },
        "Dnc" => new[] { (1320u, "Technical Finish", 20f, false), (1321u, "Standard Finish", 60f, false), (1322u, "Devilment", 20f, false) },
        "Blm" => new[] { (1400u, "Ley Lines", 30f, false), (1401u, "Triplecast", 15f, false) },
        "Smn" => new[] { (1410u, "Searing Light", 30f, false) },
        "Rdm" => new[] { (1420u, "Embolden", 20f, false), (1421u, "Manafication", 10f, false) },
        "Pct" => new[] { (1430u, "Tempera Coat", 10f, false), (1431u, "Star Prism", 20f, false) },
        _ => Array.Empty<(uint, string, float, bool)>(),
    };

    // Returns (StatusId, Name, Duration, IsDoT)
    internal static (uint, string, float, bool)[] GetJobDebuffs(string job) => job switch
    {
        "War" => new[] { (2001u, "Storm's Eye", 30f, false) },
        "Pld" => new[] { (2010u, "Goring Blade", 21f, true) },
        "Drk" => new[] { (2020u, "Salted Earth", 15f, true) },
        "Gnb" => new[] { (2030u, "Sonic Break", 30f, true) },
        "Whm" => new[] { (2100u, "Dia", 30f, true) },
        "Sch" => new[] { (2110u, "Biolysis", 30f, true), (2111u, "Chain Stratagem", 15f, false) },
        "Ast" => new[] { (2120u, "Combust III", 30f, true) },
        "Sge" => new[] { (2130u, "Eukrasian Dosis III", 30f, true) },
        "Mnk" => new[] { (2200u, "Demolish", 18f, true) },
        "Drg" => new[] { (2210u, "Chaotic Spring", 24f, true) },
        "Nin" => new[] { (2220u, "Mug", 20f, false) },
        "Sam" => new[] { (2230u, "Higanbana", 60f, true) },
        "Rpr" => new[] { (2240u, "Death's Design", 30f, false) },
        "Vpr" => new[] { (2250u, "Noxious Gnash", 20f, true) },
        "Brd" => new[] { (2300u, "Caustic Bite", 45f, true), (2301u, "Stormbite", 45f, true) },
        "Mch" => new[] { (2310u, "Wildfire", 10f, false) },
        "Dnc" => new[] { (2320u, "Closed Position", 60f, false) },
        "Blm" => new[] { (2400u, "Thunder III", 30f, true) },
        "Smn" => Array.Empty<(uint, string, float, bool)>(),
        "Rdm" => Array.Empty<(uint, string, float, bool)>(),
        "Pct" => Array.Empty<(uint, string, float, bool)>(),
        _ => Array.Empty<(uint, string, float, bool)>(),
    };
}
