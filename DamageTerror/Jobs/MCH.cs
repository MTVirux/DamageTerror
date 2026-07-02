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
}
