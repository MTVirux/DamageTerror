namespace DamageTerror.Jobs;

public sealed class MNK : JobDefinitionBase
{
    public override string Abbreviation => "Mnk";
    public override string FullName => "Monk";
    public override JobRole Role => JobRole.MeleeDps;
    public override uint ClassJobId => 20;
    public override Vector4 DefaultColor => new(0.8392157f, 0.6117647f, 0.0f, 1.0f);

    public override IReadOnlyList<PositionalFallbackEntry> FallbackPositionals { get; } =
    [
        new(56, "Snap Punch", "Flank", [(0, false), (16, false), (25, false), (17, true), (27, true), (20, true), (30, true)]),
        new(66, "Demolish", "Rear", [(0, false), (15, true), (18, true)]),
        new(36947, "Pouncing Coeurl", "Flank", [(0, false), (23, false), (15, true), (18, true), (12, true), (14, true)]),
    ];
}
