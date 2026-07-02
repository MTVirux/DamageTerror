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
        { 163, 50 },   // Thunder III
        { 1210, 35 },  // Thunder IV
        { 3871, 60 },  // High Thunder
        { 3872, 40 },  // High Thunder II
    };

    public override IReadOnlyDictionary<uint, int> DotInitialHitPotencies { get; } = new Dictionary<uint, int>
    {
        { 163, 120 },  // Thunder III
        { 1210, 80 },  // Thunder IV
        { 3871, 150 }, // High Thunder
        { 3872, 100 },  // High Thunder II
    };
}
