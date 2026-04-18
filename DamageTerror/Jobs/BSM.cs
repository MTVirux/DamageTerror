namespace DamageTerror.Jobs;

public sealed class BSM : JobDefinitionBase
{
    public override string Abbreviation => "Bsm";
    public override string FullName => "Blacksmith";
    public override JobRole Role => JobRole.DoHL;
    public override uint ClassJobId => 9;
    public override Vector4 DefaultColor => new(0.70f, 0.55f, 0.30f, 1.0f);
}
