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
}
