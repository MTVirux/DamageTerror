namespace DamageTerror.Gui.ConfigWindow;

internal static class SampleDataGenerator
{
    private static readonly Random Rng = new();

    private static readonly string[] AllJobs =
        { "War", "Pld", "Drk", "Gnb", "Whm", "Sch", "Ast", "Sge", "Mnk", "Drg", "Nin", "Sam", "Rpr", "Vpr", "Brd", "Mch", "Dnc", "Blm", "Smn", "Rdm", "Pct" };

    private static readonly string[] FirstNames =
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

    private static readonly string[] LastNames =
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

    public static EncounterSnapshot CreateFullParty()
    {
        var combatants = new List<CombatantEntry>
        {
            MakeCombatant("Vermillion Terror", "Mch", 28200, 0, isLocal: true),
            MakeCombatant("Rtb Baytolachefe", "Pld", 14500, 0),
            MakeCombatant("Marcelo Benevides", "Whm", 8000, 18500),
            MakeCombatant("Red Diamond", "Drg", 26500, 0),
            MakeCombatant("Atrina Vermillion", "Rpr", 29200, 0),
            MakeCombatant("Nikita Airisu", "Sge", 7800, 0),
            MakeCombatant("Kotoshiro Dazaria", "War", 15600, 0),
            MakeCombatant("Nestfexia Reanna", "Rdm", 30100, 4200),
            MakeCombatant("Limit Break", "Lmb", 2200, 0),
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
            MakeCombatant("Limit Break", "Lmb", 240, 0),
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
            ("Nik Baldking", "Gnb"), ("Morning Dew", "Ast"),
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

        combatants.Add(MakeCombatant("Limit Break", "Lmb", 3700, 0));

        return BuildSnapshot(combatants, "The Cloud of Darkness", "The World of Darkness", "12:45");
    }

    public static EncounterSnapshot CreateFrontline()
    {
        var combatants = new List<CombatantEntry>();

        var pvpNames = new[]
        {
            // Maelstrom (24)
            ("Kotoshiro Dazaria", "War"), ("Rtb Baytolachefe", "Pld"), ("Gale Runner", "Nin"), ("Vermillion Terrorr", "Blm"),
            ("Nik Baldking", "Gnb"), ("Marcelo Benevides", "Whm"), ("Red Diamond", "Drg"), ("Riptide Shot", "Mch"),
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
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < 200; i++)
        {
            var name = GenerateUniqueName(usedNames, i);
            var job = AllJobs[Rng.Next(AllJobs.Length)];
            var (dps, hps) = RollStats(job);
            combatants.Add(MakeCombatant(name, job, dps, hps, isLocal: i == 0));
        }

        return BuildSnapshot(combatants, "Stolas (S Rank)", "Urqopacha", "01:45");
    }

    public static (EncounterSnapshot Snapshot, Func<CombatantEntry?> Factory) CreateStressTest()
    {
        const string stressDuration = "05:00";
        var durationSec = DurationHelper.ParseDuration(stressDuration, 60f);

        // Create the first combatant immediately
        var first = MakeCombatant($"{FirstNames[0]} {LastNames[0]}", AllJobs[0], 15000, 0, isLocal: true);
        var initial = new List<CombatantEntry> { first };
        var snapshot = BuildSnapshot(initial, "Stress Test (9999 Players)", "Mor Dhona", stressDuration);

        // Lazy factory generates one combatant per call, no upfront cost
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { first.Name };
        var generated = 1;
        const int maxCount = 9999;

        Func<CombatantEntry?> factory = () =>
        {
            if (generated >= maxCount) return null;

            var name = GenerateUniqueName(usedNames, generated);
            generated++;
            var job = AllJobs[Rng.Next(AllJobs.Length)];
            var (dps, hps) = RollStats(job);
            var entry = MakeCombatant(name, job, dps, hps);
            entry.Damage = (long)(dps * durationSec);
            entry.Healed = (long)(hps * durationSec);
            FinalizeCombatant(entry, durationSec);
            return entry;
        };

        return (snapshot, factory);
    }

    private static (double Dps, double Hps) RollStats(string job)
    {
        var isHealer = job is "Whm" or "Sch" or "Ast" or "Sge";
        var isTank = job is "War" or "Pld" or "Drk" or "Gnb";
        var dps = isHealer ? Rng.Next(2000, 5000) : isTank ? Rng.Next(8000, 16000) : Rng.Next(14000, 32000);
        var hps = isHealer ? Rng.Next(6000, 14000) : 0;
        return (dps, hps);
    }

    private static string GenerateUniqueName(HashSet<string> usedNames, int suffixSeed)
    {
        string name;
        var attempts = 0;
        do
        {
            var first = FirstNames[Rng.Next(FirstNames.Length)];
            var last = LastNames[Rng.Next(LastNames.Length)];
            name = $"{first} {last}";
            attempts++;
            if (attempts > 20)
            {
                // Append number suffix to guarantee uniqueness past pool exhaustion
                name = $"{first} {last}{suffixSeed}";
            }
        } while (!usedNames.Add(name));

        return name;
    }

    private static EncounterSnapshot BuildSnapshot(
        List<CombatantEntry> combatants, string title, string zone, string duration)
    {
        var durationSec = DurationHelper.ParseDuration(duration, 60f);
        long totalDamage = 0;
        long totalHealed = 0;
        long totalDamageTaken = 0;
        int totalDeaths = 0;
        int totalKills = 0;

        foreach (var c in combatants)
        {
            c.Damage = (long)(c.EncDps * durationSec);
            c.Healed = (long)(c.EncHps * durationSec);
            FinalizeCombatant(c, durationSec);
            totalDamage += c.Damage;
            totalHealed += c.Healed;
            totalDamageTaken += c.DamageTaken;
            totalDeaths += c.Deaths;
            totalKills += c.Kills;
        }

        var totalDps = durationSec > 0 ? totalDamage / durationSec : 0;
        var totalHps = durationSec > 0 ? totalHealed / durationSec : 0;

        DistributeHealsTaken(combatants, totalHealed);

        foreach (var c in combatants)
        {
            c.DamagePercent = SimulatorHelpers.FormatPercent(c.Damage, totalDamage);
            c.HealedPercent = SimulatorHelpers.FormatPercent(c.Healed, totalHealed);
            c.DamageTakenPercent = SimulatorHelpers.FormatPercent(c.DamageTaken, totalDamageTaken);
            c.RaidDps = totalDps;
            c.RaidHps = totalHps;
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
                Kills = totalKills,
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

        foreach (var c in combatants)
        {
            snapshot.DamageTakenEvents[c.Name] = GenerateDamageTakenEvents(c, durationSec);
            snapshot.ItemEvents[c.Name] = GenerateItemEvents(durationSec);
        }

        // Fills SkillEvents from the per-combatant skill lists so graph markers have data.
        snapshot.ValidateAndRepair();

        return snapshot;
    }

    private static CombatantEntry MakeCombatant(
        string name, string job, double dps, double hps, bool isLocal = false)
    {
        var critPct = 15.0 + Rng.NextDouble() * 20.0;
        var dhPct = 20.0 + Rng.NextDouble() * 20.0;
        var overhealPct = hps > 0 ? 10.0 + Rng.NextDouble() * 30.0 : 0;

        var entry = new CombatantEntry
        {
            Name = name,
            Job = job,
            EncDps = dps,
            EncHps = hps,
            CritPct = Math.Round(critPct, 1),
            DirectHitPct = Math.Round(dhPct, 1),
            Deaths = Rng.NextDouble() < 0.15 ? Rng.Next(1, 3) : 0,
            OverhealPct = Math.Round(overhealPct, 1),
            IsLocalPlayer = isLocal,
            PeakDps = dps * (1.1 + Rng.NextDouble() * 0.3),
            InstantDps = dps * (0.85 + Rng.NextDouble() * 0.3),
            InstantHps = hps * (0.85 + Rng.NextDouble() * 0.3),
            HomeWorld = SampleJobData.Worlds[Rng.Next(SampleJobData.Worlds.Length)],
        };

        entry.Skills = GenerateSkills(job, isDamage: true);
        entry.HealingSkills = hps > 0 ? GenerateSkills(job, isDamage: false) : new();

        return entry;
    }

    /// <summary>Fills in every counter that depends on the encounter length, deriving them
    /// from each other so the numbers add up (swings = hits + misses, skill totals sum to
    /// damage, crit counts match crit percentages, ...).</summary>
    private static void FinalizeCombatant(CombatantEntry c, float durationSec)
    {
        var role = JobRegistry.GetRole(c.Job);
        var isTank = role == JobRole.Tank;
        var isHealer = role == JobRole.Healer;
        var isMelee = role is JobRole.MeleeDps or JobRole.Tank;
        var isCaster = role is JobRole.CasterDps or JobRole.Healer;

        c.CombatantDuration = SimulatorHelpers.FormatDuration(durationSec);

        c.Swings = Math.Max(1, (int)(durationSec / 2.4f * (0.85 + Rng.NextDouble() * 0.3)));
        c.Misses = (int)(c.Swings * (0.01 + Rng.NextDouble() * 0.04));
        c.Hits = c.Swings - c.Misses;
        c.HitRate = SimulatorHelpers.Percent(c.Hits, c.Swings);

        c.CritHitCount = (int)(c.Hits * c.CritPct / 100.0);
        c.DirectHitCount = (int)(c.Hits * c.DirectHitPct / 100.0);
        c.CritDirectHitCount = (int)(Math.Min(c.CritHitCount, c.DirectHitCount) * (0.3 + Rng.NextDouble() * 0.2));
        c.CritPct = SimulatorHelpers.Percent(c.CritHitCount, c.Hits);
        c.DirectHitPct = SimulatorHelpers.Percent(c.DirectHitCount, c.Hits);
        c.CritDirectHitPct = SimulatorHelpers.Percent(c.CritDirectHitCount, c.Hits);

        if (isMelee)
        {
            c.PositionalHits = (int)(c.Hits * 0.3 * (0.7 + Rng.NextDouble() * 0.3));
            c.PositionalMisses = (int)(c.PositionalHits * Rng.NextDouble() * 0.25);
            c.Positionals = c.PositionalHits + c.PositionalMisses;
        }

        c.BlockPct = isTank ? Math.Round(8.0 + Rng.NextDouble() * 18.0, 1) : 0;
        c.ParryPct = isTank ? Math.Round(12.0 + Rng.NextDouble() * 20.0, 1) : 0;

        c.DamageTaken = (long)(durationSec * (isTank ? Rng.Next(1500, 3200) : Rng.Next(300, 900)));
        c.Kills = Rng.Next(0, 4);
        c.Stuns = Rng.Next(0, 3);
        c.SkillIssue = Rng.NextDouble() < 0.35 ? Rng.Next(1, 4) : 0;
        c.DamageDown = Rng.NextDouble() < 0.3 ? Rng.Next(1, 3) : 0;
        c.PowerDrain = isCaster ? (long)(durationSec * Rng.Next(20, 60)) : 0;
        c.PowerHeal = isCaster ? (long)(durationSec * Rng.Next(10, 40)) : 0;

        ScaleSkills(c.Skills, c.Damage);
        ScaleSkills(c.HealingSkills, c.Healed);

        c.MaxHitDamage = BiggestAmount(c.Damage, c.Hits);
        c.MaxHit = SimulatorHelpers.FormatMaxLabel(
            TopSkillName(c.Skills, SampleJobData.GetMaxHitSkill(c.Job)), c.MaxHitDamage);

        c.HealCount = TotalHits(c.HealingSkills);
        if (c.Healed > 0)
        {
            c.MaxHealAmount = BiggestAmount(c.Healed, c.HealCount);
            c.MaxHeal = SimulatorHelpers.FormatMaxLabel(TopSkillName(c.HealingSkills, "Cure"), c.MaxHealAmount);
            c.CritHealPct = Math.Round(10.0 + Rng.NextDouble() * 15.0, 1);
            c.OverhealAmount = (long)(c.Healed * (c.OverhealPct / Math.Max(1.0, 100.0 - c.OverhealPct)));
            c.OverhealPct = SimulatorHelpers.OverhealPct(c.Healed, c.OverhealAmount);
        }

        if (isHealer || isTank)
        {
            c.DamageShield = (long)(Math.Max(c.Healed, c.Damage / 20) * (0.15 + Rng.NextDouble() * 0.35));
            c.AbsorbHeal = (long)(c.DamageShield * (0.5 + Rng.NextDouble() * 0.4));
            c.MaxHealWardAmount = BiggestAmount(c.DamageShield, Math.Max(1, c.HealCount / 2));
            c.MaxHealWardName = TopSkillName(c.HealingSkills, "Adloquium");
        }
    }

    private static int TotalHits(List<SkillEntry> skills)
    {
        var hits = 0;
        foreach (var s in skills)
        {
            hits += s.HitCount;
            if (s.SubEntries != null)
                foreach (var sub in s.SubEntries) hits += sub.HitCount;
        }
        return hits;
    }

    private static long BiggestAmount(long total, int hits)
        => hits > 0 ? (long)(total / (double)hits * (2.5 + Rng.NextDouble() * 2.0)) : 0;

    private static string TopSkillName(List<SkillEntry> skills, string fallback)
    {
        var best = fallback;
        long bestAmount = 0;
        foreach (var s in skills)
        {
            if (s.TotalDamage <= bestAmount) continue;
            bestAmount = s.TotalDamage;
            best = s.Name;
        }
        return best;
    }

    /// <summary>Rescales generated skill totals so they sum to exactly the combatant's damage / healing.</summary>
    private static void ScaleSkills(List<SkillEntry> skills, long target)
    {
        if (skills.Count == 0) return;

        if (target <= 0)
        {
            skills.Clear();
            return;
        }

        long raw = 0;
        foreach (var s in skills)
        {
            raw += s.TotalDamage;
            if (s.SubEntries != null)
                foreach (var sub in s.SubEntries) raw += sub.TotalDamage;
        }
        if (raw <= 0) return;

        var factor = (double)target / raw;
        long assigned = 0;
        foreach (var s in skills)
        {
            s.TotalDamage = (long)(s.TotalDamage * factor);
            assigned += s.TotalDamage;
            if (s.SubEntries == null) continue;
            foreach (var sub in s.SubEntries)
            {
                sub.TotalDamage = (long)(sub.TotalDamage * factor);
                assigned += sub.TotalDamage;
            }
        }

        skills[0].TotalDamage += target - assigned;
        SimulatorHelpers.RecomputeSkillPercents(skills);
    }

    private static void DistributeHealsTaken(List<CombatantEntry> combatants, long totalHealed)
    {
        if (combatants.Count == 0 || totalHealed <= 0) return;

        var weights = new double[combatants.Count];
        var weightSum = 0.0;
        for (var i = 0; i < weights.Length; i++)
        {
            weights[i] = 0.5 + Rng.NextDouble();
            weightSum += weights[i];
        }

        long assigned = 0;
        for (var i = 0; i < combatants.Count; i++)
        {
            combatants[i].HealsTaken = (long)(totalHealed * (weights[i] / weightSum));
            assigned += combatants[i].HealsTaken;
        }
        combatants[0].HealsTaken += totalHealed - assigned;
    }

    private static List<SkillUseEvent> GenerateItemEvents(float durationSec)
    {
        var events = new List<SkillUseEvent>();
        var uses = Rng.Next(1, 4);
        for (var i = 0; i < uses; i++)
        {
            events.Add(new SkillUseEvent
            {
                TimeSec = (float)(Rng.NextDouble() * durationSec),
                SkillName = SampleJobData.Items[Rng.Next(SampleJobData.Items.Length)],
            });
        }
        events.Sort((a, b) => a.TimeSec.CompareTo(b.TimeSec));
        return events;
    }

    private static List<SkillUseEvent> GenerateDamageTakenEvents(CombatantEntry c, float durationSec)
    {
        var events = new List<SkillUseEvent>();
        if (c.DamageTaken <= 0) return events;

        var count = Math.Max(1, (int)(durationSec / 8f));
        var weights = new double[count];
        var weightSum = 0.0;
        for (var i = 0; i < count; i++)
        {
            weights[i] = 0.4 + Rng.NextDouble();
            weightSum += weights[i];
        }

        long assigned = 0;
        for (var i = 0; i < count; i++)
        {
            var amount = (long)(c.DamageTaken * (weights[i] / weightSum));
            if (i == count - 1) amount = c.DamageTaken - assigned;
            assigned += amount;

            events.Add(new SkillUseEvent
            {
                TimeSec = durationSec * (i + 0.5f) / count,
                SkillName = SampleJobData.BossSkills[Rng.Next(SampleJobData.BossSkills.Length)],
                Amount = amount,
            });
        }
        return events;
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

        AttachTickEntries(skills, job, isDamage);
        return skills;
    }

    /// <summary>Nests a tick breakdown under any skill that has a matching DoT / HoT status,
    /// the same way the live skill list does.</summary>
    private static void AttachTickEntries(List<SkillEntry> skills, string job, bool isDamage)
    {
        var label = isDamage ? "DoT" : "HoT";
        var tickNames = isDamage
            ? SampleJobData.GetJobDebuffs(job).Where(d => d.IsDot).Select(d => d.Name)
            : SampleJobData.GetJobBuffs(job).Where(b => b.IsHoT).Select(b => b.Name);

        foreach (var tickName in tickNames)
        {
            var parent = skills.Find(s => s.Name == tickName);
            if (parent == null || parent.HitCount <= 0) continue;

            parent.SubEntries = new List<SkillEntry>
            {
                new()
                {
                    Name = $"{tickName} ({label})",
                    TotalDamage = (long)(parent.TotalDamage * (1.5 + Rng.NextDouble())),
                    HitCount = parent.HitCount * Rng.Next(4, 9),
                    DamageType = SkillDamageType.Magic,
                    CritPct = parent.CritPct,
                    DirectHitPct = parent.DirectHitPct,
                    CritDirectHitPct = parent.CritDirectHitPct,
                },
            };
        }
    }

    private static string[] GetDamageSkillNames(string job) => SampleJobData.GetDamageSkillNames(job);

    private static string[] GetHealSkillNames(string job) => SampleJobData.GetHealSkillNames(job);

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

    internal static (uint, string, float, bool)[] GetJobBuffs(string job) => SampleJobData.GetJobBuffs(job);

    internal static (uint, string, float, bool)[] GetJobDebuffs(string job) => SampleJobData.GetJobDebuffs(job);
}
