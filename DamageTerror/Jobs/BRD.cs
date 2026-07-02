namespace DamageTerror.Jobs;

public sealed class BRD : JobDefinitionBase
{
    public override string Abbreviation => "Brd";
    public override string FullName => "Bard";
    public override JobRole Role => JobRole.RangedDps;
    public override uint ClassJobId => 23;
    public override Vector4 DefaultColor => new(0.5686275f, 0.7294118f, 0.36862746f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 124, 15 },   // Venomous Bite
        { 129, 20 },   // Windbite
        { 1200, 20 },  // Caustic Bite
        { 1201, 25 },  // Stormbite
    };

    public override IReadOnlyDictionary<uint, int> DotInitialHitPotencies { get; } = new Dictionary<uint, int>
    {
        { 1200, 150 }, // Caustic Bite
        { 1201, 100 }, // Stormbite
    };
}
