namespace DamageTerror.Jobs;

public sealed class RDM : JobDefinitionBase
{
    public override string Abbreviation => "Rdm";
    public override string FullName => "Red Mage";
    public override JobRole Role => JobRole.CasterDps;
    public override uint ClassJobId => 35;
    public override Vector4 DefaultColor => new(0.9098039f, 0.48235294f, 0.48235294f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 4319, 65 },  // Scorch (PvP)
    };
}
