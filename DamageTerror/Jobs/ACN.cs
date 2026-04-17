namespace DamageTerror.Jobs;

public sealed class ACN : JobDefinitionBase
{
    public override string Abbreviation => "Acn";
    public override string FullName => "Arcanist";
    public override JobRole Role => JobRole.CasterDps;
    public override uint ClassJobId => 26;
    public override Vector4 DefaultColor => new(0.1764706f, 0.60784316f, 0.47058824f, 1.0f);
    public override bool IsBaseClass => true;
}
