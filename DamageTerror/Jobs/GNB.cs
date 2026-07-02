namespace DamageTerror.Jobs;

public sealed class GNB : JobDefinitionBase
{
    public override string Abbreviation => "Gnb";
    public override string FullName => "Gunbreaker";
    public override JobRole Role => JobRole.Tank;
    public override uint ClassJobId => 37;
    public override Vector4 DefaultColor => new(0.47450981f, 0.42745098f, 0.18823530f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 1837, 120 },  // Sonic Break
        { 1838, 60 },  // Bow Shock
    };

    public override IReadOnlyDictionary<uint, int> DotInitialHitPotencies { get; } = new Dictionary<uint, int>
    {
        { 1837, 340 }, // Sonic Break
        { 1838, 150 }, // Bow Shock
    };

    public override IReadOnlyDictionary<uint, int> HotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 1835, 200 }, // Aurora
    };
}
