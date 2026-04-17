namespace DamageTerror.Jobs;

public sealed class PGL : JobDefinitionBase
{
    public override string Abbreviation => "Pgl";
    public override string FullName => "Pugilist";
    public override JobRole Role => JobRole.MeleeDps;
    public override uint ClassJobId => 2;
    public override Vector4 DefaultColor => new(0.8392157f, 0.6117647f, 0.0f, 1.0f);
    public override bool IsBaseClass => true;
}
