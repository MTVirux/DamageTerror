namespace DamageTerror.Gui.ConfigWindow;

// Fabricated preview fixtures for the sample-data generators. Keyed by job abbreviation.
internal static class SampleJobData
{
    public readonly record struct Entry(
        string MaxHitSkill,
        string[] DamageSkillNames,
        string[] HealSkillNames,
        (uint Id, string Name, float Duration, bool IsHoT)[] Buffs,
        (uint Id, string Name, float Duration, bool IsDot)[] Debuffs);

    private const string DefaultMaxHitSkill = "Attack";
    private static readonly string[] DefaultDamageSkillNames = ["Attack", "Auto-Attack"];
    private static readonly string[] DefaultHealSkillNames = ["Second Wind", "Bloodbath"];
    private static readonly (uint Id, string Name, float Duration, bool IsHoT)[] NoBuffs = [];
    private static readonly (uint Id, string Name, float Duration, bool IsDot)[] NoDebuffs = [];

    private static readonly Dictionary<string, Entry> Table = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Pld"] = new(
            "Confiteor",
            ["Confiteor", "Blade of Honor", "Holy Spirit", "Atonement", "Goring Blade", "Royal Authority"],
            DefaultHealSkillNames,
            [(1010u, "Sentinel", 15f, false), (1011u, "Divine Veil", 30f, false), (1012u, "Hallowed Ground", 10f, false)],
            [(2010u, "Goring Blade", 21f, true)]),

        ["War"] = new(
            "Primal Rend",
            ["Primal Rend", "Inner Chaos", "Fell Cleave", "Upheaval", "Onslaught", "Storm's Eye"],
            DefaultHealSkillNames,
            [(1001u, "Vengeance", 15f, false), (1002u, "Thrill of Battle", 10f, false), (1003u, "Shake It Off", 30f, false)],
            [(2001u, "Storm's Eye", 30f, false)]),

        ["Drk"] = new(
            "Living Shadow",
            ["Living Shadow", "Shadowbringer", "Edge of Shadow", "Bloodspiller", "Carve and Spit", "Souleater"],
            DefaultHealSkillNames,
            [(1020u, "Shadow Wall", 15f, false), (1021u, "Dark Mind", 10f, false), (1022u, "The Blackest Night", 7f, false)],
            [(2020u, "Salted Earth", 15f, true)]),

        ["Gnb"] = new(
            "Blasting Zone",
            ["Blasting Zone", "Double Down", "Sonic Break", "Burst Strike", "Gnashing Fang", "Hypervelocity"],
            DefaultHealSkillNames,
            [(1030u, "Nebula", 15f, false), (1031u, "Camouflage", 20f, false), (1032u, "Heart of Corundum", 8f, false)],
            [(2030u, "Sonic Break", 30f, true)]),

        ["Whm"] = new(
            "Glare III",
            ["Glare III", "Afflatus Misery", "Dia", "Assize", "Holy III"],
            ["Medica II", "Afflatus Rapture", "Afflatus Solace", "Cure III", "Regen", "Liturgy of the Bell"],
            [(1100u, "Regen", 18f, true), (1101u, "Medica II", 15f, true), (1102u, "Temperance", 22f, false), (1103u, "Liturgy of the Bell", 20f, false)],
            [(2100u, "Dia", 30f, true)]),

        ["Sch"] = new(
            "Broil IV",
            ["Broil IV", "Biolysis", "Energy Drain", "Chain Stratagem", "Art of War II"],
            ["Adloquium", "Succor", "Lustrate", "Excogitation", "Sacred Soil", "Seraphic Veil"],
            [(1110u, "Galvanize", 30f, false), (1111u, "Sacred Soil", 15f, true), (1112u, "Expedient", 20f, false), (1113u, "Seraphic Veil", 30f, false)],
            [(2110u, "Biolysis", 30f, true), (2111u, "Chain Stratagem", 15f, false)]),

        ["Ast"] = new(
            "Fall Malefic",
            ["Fall Malefic", "Combust III", "Lord of Crowns", "Earthly Star", "Gravity II"],
            ["Aspected Benefic", "Aspected Helios", "Celestial Opposition", "Earthly Star", "Essential Dignity", "Macrocosmos"],
            [(1120u, "Aspected Benefic", 15f, true), (1121u, "Aspected Helios", 15f, true), (1122u, "Earthly Star", 20f, false), (1123u, "The Arrow", 15f, false), (1124u, "The Balance", 15f, false)],
            [(2120u, "Combust III", 30f, true)]),

        ["Sge"] = new(
            "Pneuma",
            ["Dosis III", "Eukrasian Dosis III", "Phlegma III", "Toxikon II", "Pneuma"],
            ["Eukrasian Diagnosis", "Eukrasian Prognosis", "Druochole", "Kerachole", "Ixochole", "Pneuma"],
            [(1130u, "Eukrasian Diagnosis", 30f, false), (1131u, "Kerachole", 15f, true), (1132u, "Holos", 20f, false), (1133u, "Physis II", 15f, true)],
            [(2130u, "Eukrasian Dosis III", 30f, true)]),

        ["Mnk"] = new(
            "Phantom Rush",
            ["Phantom Rush", "Elixir Burst", "Rising Phoenix", "Bootshine", "Dragon Kick", "Demolish"],
            DefaultHealSkillNames,
            [(1200u, "Brotherhood", 20f, false), (1201u, "Mantra", 15f, false)],
            [(2200u, "Demolish", 18f, true)]),

        ["Drg"] = new(
            "Stardiver",
            ["Stardiver", "Nastrond", "Heaven's Thrust", "Chaotic Spring", "Wyrmwind Thrust", "Dragonfire Dive"],
            DefaultHealSkillNames,
            [(1210u, "Battle Litany", 20f, false), (1211u, "Dragon Sight", 20f, false)],
            [(2210u, "Chaotic Spring", 24f, true)]),

        ["Nin"] = new(
            "Hyosho Ranryu",
            ["Hyosho Ranryu", "Forked Raijin", "Fleeting Raijin", "Bhavacakra", "Aeolian Edge", "Armor Crush"],
            DefaultHealSkillNames,
            [(1220u, "Trick Attack", 15f, false)],
            [(2220u, "Mug", 20f, false)]),

        ["Sam"] = new(
            "Midare Setsugekka",
            ["Midare Setsugekka", "Ogi Namikiri", "Kaeshi: Namikiri", "Higanbana", "Shinten", "Shoha"],
            DefaultHealSkillNames,
            [(1230u, "Meikyo Shisui", 15f, false)],
            [(2230u, "Higanbana", 60f, true)]),

        ["Rpr"] = new(
            "Communio",
            ["Communio", "Plentiful Harvest", "Gibbet", "Gallows", "Void Reaping", "Cross Reaping"],
            DefaultHealSkillNames,
            [(1240u, "Arcane Circle", 20f, false)],
            [(2240u, "Death's Design", 30f, false)]),

        ["Vpr"] = new(
            "Ouroboros",
            ["Ouroboros", "Reawaken", "Uncoiled Fury", "Hindsting Strike", "Flanksbane Fang", "Hunter's Sting"],
            DefaultHealSkillNames,
            [(1250u, "Serpent's Ire", 15f, false)],
            [(2250u, "Noxious Gnash", 20f, true)]),

        ["Brd"] = new(
            "Radiant Finale",
            ["Radiant Finale", "Blast Arrow", "Apex Arrow", "Refulgent Arrow", "Burst Shot", "Iron Jaws"],
            DefaultHealSkillNames,
            [(1300u, "Mage's Ballad", 45f, false), (1301u, "Army's Paeon", 45f, false), (1302u, "The Wanderer's Minuet", 45f, false), (1303u, "Radiant Finale", 20f, false)],
            [(2300u, "Caustic Bite", 45f, true), (2301u, "Stormbite", 45f, true)]),

        ["Mch"] = new(
            "Wildfire",
            ["Wildfire", "Chain Saw", "Excavator", "Air Anchor", "Drill", "Heat Blast"],
            DefaultHealSkillNames,
            [(1310u, "Reassemble", 5f, false)],
            [(2310u, "Wildfire", 10f, false)]),

        ["Dnc"] = new(
            "Technical Finish",
            ["Technical Finish", "Starfall Dance", "Saber Dance", "Tillana", "Standard Finish", "Fan Dance IV"],
            DefaultHealSkillNames,
            [(1320u, "Technical Finish", 20f, false), (1321u, "Standard Finish", 60f, false), (1322u, "Devilment", 20f, false)],
            [(2320u, "Closed Position", 60f, false)]),

        ["Blm"] = new(
            "Flare Star",
            ["Flare Star", "Despair", "Xenoglossy", "Fire IV", "Paradox", "Thunder III"],
            DefaultHealSkillNames,
            [(1400u, "Ley Lines", 30f, false), (1401u, "Triplecast", 15f, false)],
            [(2400u, "Thunder III", 30f, true)]),

        ["Smn"] = new(
            "Akh Morn",
            ["Akh Morn", "Enkindle Bahamut", "Astral Impulse", "Ruby Rite", "Topaz Rite", "Emerald Rite"],
            DefaultHealSkillNames,
            [(1410u, "Searing Light", 30f, false)],
            NoDebuffs),

        ["Rdm"] = new(
            "Scorch",
            ["Scorch", "Resolution", "Verholy", "Verflare", "Fleche", "Contre Sixte"],
            DefaultHealSkillNames,
            [(1420u, "Embolden", 20f, false), (1421u, "Manafication", 10f, false)],
            NoDebuffs),

        ["Pct"] = new(
            "Star Prism",
            ["Star Prism", "Comet in Black", "Holy in White", "Fire in Red", "Creature Motif", "Hammer Stamp"],
            DefaultHealSkillNames,
            [(1430u, "Tempera Coat", 10f, false), (1431u, "Star Prism", 20f, false)],
            NoDebuffs),
    };

    public static readonly string[] BossSkills =
    {
        "Akh Morn", "Megaflare", "Exaflare", "Diamond Dust", "Earthen Fury",
        "Hellfire", "Judgment Bolt", "Tidal Wave", "Aerial Blast", "Cauterize",
        "Tera Slash", "Giga Slash", "Wave Cannon", "Ion Efflux", "Atomic Ray",
    };

    public static readonly string[] Items =
    {
        "Grade 8 Tincture of Strength", "Grade 8 Tincture of Dexterity",
        "Grade 8 Tincture of Intelligence", "Grade 8 Tincture of Mind",
        "Hi-Elixir", "Super-Potion", "Baked Eggplant", "Vermillion Cloak",
    };

    public static readonly string[] Worlds =
    {
        "Spriggan", "Twintania", "Alpha", "Raiden", "Lich", "Odin", "Phoenix", "Shiva",
        "Zodiark", "Cerberus", "Louisoix", "Moogle", "Omega", "Ragnarok", "Sagittarius",
    };

    public static string GetMaxHitSkill(string job) =>
        Table.TryGetValue(job, out var e) ? e.MaxHitSkill : DefaultMaxHitSkill;

    public static string[] GetDamageSkillNames(string job) =>
        Table.TryGetValue(job, out var e) ? e.DamageSkillNames : DefaultDamageSkillNames;

    public static string[] GetHealSkillNames(string job) =>
        Table.TryGetValue(job, out var e) ? e.HealSkillNames : DefaultHealSkillNames;

    public static (uint Id, string Name, float Duration, bool IsHoT)[] GetJobBuffs(string job) =>
        Table.TryGetValue(job, out var e) ? e.Buffs : NoBuffs;

    public static (uint Id, string Name, float Duration, bool IsDot)[] GetJobDebuffs(string job) =>
        Table.TryGetValue(job, out var e) ? e.Debuffs : NoDebuffs;
}
