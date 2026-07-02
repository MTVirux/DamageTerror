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
        { 1895, 85 },  // Biolysis
        { 189, 40 },   // Bio II
        { 3883, 140 },  // Baneful Impaction
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
}
