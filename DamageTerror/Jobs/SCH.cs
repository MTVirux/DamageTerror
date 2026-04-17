namespace DamageTerror.Jobs;

public sealed class SCH : JobDefinitionBase
{
    public override string Abbreviation => "Sch";
    public override string FullName => "Scholar";
    public override JobRole Role => JobRole.Healer;
    public override uint ClassJobId => 28;
    public override Vector4 DefaultColor => new(0.5254902f, 0.34117648f, 1.0f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 1895, 75 },  // Biolysis
        { 189, 20 },   // Bio II
        { 3883, 50 },  // Baneful Impaction
        { 2039, 50 },  // Biolysis (PvP)
    };

    public override IReadOnlyDictionary<uint, int> DotInitialHitPotencies { get; } = new Dictionary<uint, int>
    {
        { 1895, 75 },  // Biolysis
    };

    public override IReadOnlyDictionary<uint, int> HotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 315, 120 },  // Whispering Dawn
        { 1874, 120 }, // Angel's Whisper
        { 1944, 100 }, // Sacred Soil
        { 3885, 100 }, // Seraphism
    };

    public override IReadOnlySet<uint> KnownDotStatusIds { get; } = new HashSet<uint>
    {
        1895, // Biolysis
        189,  // Bio II
        3883, // Baneful Impaction
        2039, // Biolysis (PvP)
    };

    public override IReadOnlySet<uint> KnownHotStatusIds { get; } = new HashSet<uint>
    {
        315,  // Whispering Dawn
        1874, // Angel's Whisper (Seraph)
        1944, // Sacred Soil
        3885, // Seraphism HoT
    };

    public override string MaxHitSkill => "Broil IV";

    public override string[] DamageSkillNames =>
        ["Broil IV", "Biolysis", "Energy Drain", "Chain Stratagem", "Art of War II"];

    public override string[] HealSkillNames =>
        ["Adloquium", "Succor", "Lustrate", "Excogitation", "Sacred Soil", "Seraphic Veil"];

    public override (uint Id, string Name, float Duration, bool IsHoT)[] SampleBuffs =>
        [(1110u, "Galvanize", 30f, false), (1111u, "Sacred Soil", 15f, true), (1112u, "Expedient", 20f, false), (1113u, "Seraphic Veil", 30f, false)];

    public override (uint Id, string Name, float Duration, bool IsDot)[] SampleDebuffs =>
        [(2110u, "Biolysis", 30f, true), (2111u, "Chain Stratagem", 15f, false)];
}
