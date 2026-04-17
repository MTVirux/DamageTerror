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

    public override IReadOnlySet<uint> KnownDotStatusIds { get; } = new HashSet<uint>
    {
        1714, // Bleeding (Song of Torment, Nightbloom, Aetherial Spark)
        1736, // Dropsy (Aqua Breath)
        18,   // Poison (Bad Breath)
        1723, // Windburn (Feather Rain)
        3712, // Breath of Magic
        3643, // Mortal Flame
    };

    public override IReadOnlySet<uint> KnownHotStatusIds { get; } = new HashSet<uint>
    {
        2495, // Angel's Snack
    };
}
