namespace DamageTerror.Jobs;

public sealed class DNC : JobDefinitionBase
{
    public override string Abbreviation => "Dnc";
    public override string FullName => "Dancer";
    public override JobRole Role => JobRole.RangedDps;
    public override uint ClassJobId => 38;
    public override Vector4 DefaultColor => new(0.8862745f, 0.6901961f, 0.6862745f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 3162, 75 },  // Honing Dance (PvP)
    };

    public override IReadOnlyDictionary<uint, int> HotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 2695, 100 }, // Improvisation
    };

    public override IReadOnlyDictionary<uint, string> GroundEffectDots { get; } = new Dictionary<uint, string>
    {
        { 3162, "Honing Dance" }, // PvP
    };
}
