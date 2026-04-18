namespace DamageTerror.Jobs;

public sealed class FSH : JobDefinitionBase
{
    public override string Abbreviation => "Fsh";
    public override string FullName => "Fisher";
    public override JobRole Role => JobRole.DoHL;
    public override uint ClassJobId => 18;
    public override Vector4 DefaultColor => new(0.70f, 0.55f, 0.30f, 1.0f);
}
