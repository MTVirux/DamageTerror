namespace DamageTerror.Jobs;

public sealed class RPR : JobDefinitionBase
{
    public override string Abbreviation => "Rpr";
    public override string FullName => "Reaper";
    public override JobRole Role => JobRole.MeleeDps;
    public override uint ClassJobId => 39;
    public override Vector4 DefaultColor => new(0.5882353f, 0.3529412f, 0.5647059f, 1.0f);

    public override IReadOnlyDictionary<uint, int> HotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 2862, 100 }, // Crest of Time Returned (PvP)
    };

    public override IReadOnlyList<PositionalFallbackEntry> FallbackPositionals { get; } =
    [
        new(24382, "Gibbet", "Flank", [(0, false), (10, false), (11, true), (19, true)]),
        new(24383, "Gallows", "Rear", [(0, false), (10, false), (11, true), (19, true)]),
        new(36970, "Executioner's Gibbet", "Flank", [(0, false), (7, true)]),
        new(36971, "Executioner's Gallows", "Rear", [(0, false), (7, true)]),
    ];
}
