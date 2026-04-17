namespace DamageTerror.Jobs;

public sealed class BLM : JobDefinitionBase
{
    public override string Abbreviation => "Blm";
    public override string FullName => "Black Mage";
    public override JobRole Role => JobRole.CasterDps;
    public override uint ClassJobId => 25;
    public override Vector4 DefaultColor => new(0.64705884f, 0.4745098f, 0.8392157f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 163, 35 },   // Thunder III
        { 1210, 30 },  // Thunder IV
        { 3871, 30 },  // High Thunder
        { 3872, 30 },  // High Thunder II
    };

    public override IReadOnlyDictionary<uint, int> DotInitialHitPotencies { get; } = new Dictionary<uint, int>
    {
        { 163, 120 },  // Thunder III
        { 1210, 80 },  // Thunder IV
        { 3871, 150 }, // High Thunder
        { 3872, 80 },  // High Thunder II
    };

    public override IReadOnlySet<uint> KnownDotStatusIds { get; } = new HashSet<uint>
    {
        163,  // Thunder III
        1210, // Thunder IV
        3871, // High Thunder
        3872, // High Thunder II
    };

    public override string MaxHitSkill => "Flare Star";

    public override string[] DamageSkillNames =>
        ["Flare Star", "Despair", "Xenoglossy", "Fire IV", "Paradox", "Thunder III"];

    public override (uint Id, string Name, float Duration, bool IsHoT)[] SampleBuffs =>
        [(1400u, "Ley Lines", 30f, false), (1401u, "Triplecast", 15f, false)];

    public override (uint Id, string Name, float Duration, bool IsDot)[] SampleDebuffs =>
        [(2400u, "Thunder III", 30f, true)];
}
