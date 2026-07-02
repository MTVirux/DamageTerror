namespace DamageTerror.Jobs;

public sealed class PLD : JobDefinitionBase
{
    public override string Abbreviation => "Pld";
    public override string FullName => "Paladin";
    public override JobRole Role => JobRole.Tank;
    public override uint ClassJobId => 19;
    public override Vector4 DefaultColor => new(0.65882355f, 0.8235294f, 0.9019608f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 248, 30 },   // Circle of Scorn
    };

    public override IReadOnlyDictionary<uint, int> DotInitialHitPotencies { get; } = new Dictionary<uint, int>
    {
        { 248, 140 },  // Circle of Scorn
    };

    public override IReadOnlyDictionary<uint, int> HotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 2676, 250 }, // Knight's Benediction
    };
}
