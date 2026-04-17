namespace DamageTerror.Jobs;

public sealed class MCH : JobDefinitionBase
{
    public override string Abbreviation => "Mch";
    public override string FullName => "Machinist";
    public override JobRole Role => JobRole.RangedDps;
    public override uint ClassJobId => 31;
    public override Vector4 DefaultColor => new(0.43137255f, 0.88235295f, 0.8392157f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 1866, 50 },  // Bioblaster
        { 2019, 65 },  // Bioblaster (PvP)
    };

    public override IReadOnlyDictionary<uint, int> DotInitialHitPotencies { get; } = new Dictionary<uint, int>
    {
        { 1866, 50 },  // Bioblaster
    };

    public override IReadOnlySet<uint> KnownDotStatusIds { get; } = new HashSet<uint>
    {
        1866, // Bioblaster
        2019, // Bioblaster (PvP)
    };

    public override string MaxHitSkill => "Wildfire";

    public override string[] DamageSkillNames =>
        ["Wildfire", "Chain Saw", "Excavator", "Air Anchor", "Drill", "Heat Blast"];

    public override (uint Id, string Name, float Duration, bool IsHoT)[] SampleBuffs =>
        [(1310u, "Reassemble", 5f, false)];

    public override (uint Id, string Name, float Duration, bool IsDot)[] SampleDebuffs =>
        [(2310u, "Wildfire", 10f, false)];
}
