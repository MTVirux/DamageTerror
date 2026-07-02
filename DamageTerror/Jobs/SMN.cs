namespace DamageTerror.Jobs;

public sealed class SMN : JobDefinitionBase
{
    public override string Abbreviation => "Smn";
    public override string FullName => "Summoner";
    public override JobRole Role => JobRole.CasterDps;
    public override uint ClassJobId => 27;
    public override Vector4 DefaultColor => new(0.1764706f, 0.60784316f, 0.47058824f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 2706, 30 },  // Slipstream
        { 3231, 65 },  // Scarlet Flame (PvP)
    };

    public override IReadOnlyDictionary<uint, string> GroundEffectDots { get; } = new Dictionary<uint, string>
    {
        { 2706, "Slipstream" },
    };
}
