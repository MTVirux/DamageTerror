namespace DamageTerror.Jobs;

public sealed class CRP : JobDefinitionBase
{
    public override string Abbreviation => "Crp";
    public override string FullName => "Carpenter";
    public override JobRole Role => JobRole.DoHL;
    public override uint ClassJobId => 8;
    public override Vector4 DefaultColor => new(0.70f, 0.55f, 0.30f, 1.0f);
}
