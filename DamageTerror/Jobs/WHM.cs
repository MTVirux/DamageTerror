namespace DamageTerror.Jobs;

public sealed class WHM : JobDefinitionBase
{
    public override string Abbreviation => "Whm";
    public override string FullName => "White Mage";
    public override JobRole Role => JobRole.Healer;
    public override uint ClassJobId => 24;
    public override Vector4 DefaultColor => new(1.0f, 0.9411765f, 0.8627451f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 1871, 85 },  // Dia
        { 143, 30 },   // Aero
        { 144, 50 },   // Aero II
        { 798, 50 },   // Aero III
    };

    public override IReadOnlyDictionary<uint, int> DotInitialHitPotencies { get; } = new Dictionary<uint, int>
    {
        { 1871, 85 },  // Dia
    };

    public override IReadOnlyDictionary<uint, int> HotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 158, 250 },  // Regen
        { 150, 150 },  // Medica II
        { 3880, 175 }, // Medica III
        { 1911, 100 }, // Asylum
    };

    public override IReadOnlySet<uint> KnownDotStatusIds { get; } = new HashSet<uint>
    {
        1871, // Dia
        143,  // Aero
        144,  // Aero II
        798,  // Aero III
    };

    public override IReadOnlySet<uint> KnownHotStatusIds { get; } = new HashSet<uint>
    {
        158,  // Regen
        150,  // Medica II
        3880, // Medica III
        1911, // Asylum
    };

    public override string MaxHitSkill => "Glare III";

    public override string[] DamageSkillNames =>
        ["Glare III", "Afflatus Misery", "Dia", "Assize", "Holy III"];

    public override string[] HealSkillNames =>
        ["Medica II", "Afflatus Rapture", "Afflatus Solace", "Cure III", "Regen", "Liturgy of the Bell"];

    public override (uint Id, string Name, float Duration, bool IsHoT)[] SampleBuffs =>
        [(1100u, "Regen", 18f, true), (1101u, "Medica II", 15f, true), (1102u, "Temperance", 22f, false), (1103u, "Liturgy of the Bell", 20f, false)];

    public override (uint Id, string Name, float Duration, bool IsDot)[] SampleDebuffs =>
        [(2100u, "Dia", 30f, true)];
}
