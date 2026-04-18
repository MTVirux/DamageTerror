namespace DamageTerror.Jobs;

public sealed class ARM : JobDefinitionBase
{
    public override string Abbreviation => "Arm";
    public override string FullName => "Armorer";
    public override JobRole Role => JobRole.DoHL;
    public override uint ClassJobId => 10;
    public override Vector4 DefaultColor => new(0.70f, 0.55f, 0.30f, 1.0f);
}
