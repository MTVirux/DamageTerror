namespace DamageTerror.Jobs;

public sealed class BLU : JobDefinitionBase
{
    public override string Abbreviation => "Blu";
    public override string FullName => "Blue Mage";
    public override JobRole Role => JobRole.CasterDps;
    public override uint ClassJobId => 36;
    public override Vector4 DefaultColor => new(0.30f, 0.55f, 0.90f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 1714, 50 },  // Bleeding
        { 1736, 50 },  // Dropsy
        { 18, 30 },    // Poison
        { 1723, 20 },  // Windburn
        { 3712, 80 },  // Breath of Magic
        { 3643, 50 },  // Mortal Flame
    };

    public override IReadOnlyDictionary<uint, int> HotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 2495, 100 }, // Angel's Snack
    };

    public override IReadOnlySet<uint> KnownReflectStatusIds { get; } = new HashSet<uint>
    {
        1720, // Ice Spikes
        1724, // Veil of the Whorl
        3631, // Schiltron
    };
}
