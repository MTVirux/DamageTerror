namespace DamageTerror.Jobs;

public sealed class WVR : JobDefinitionBase
{
    public override string Abbreviation => "Wvr";
    public override string FullName => "Weaver";
    public override JobRole Role => JobRole.DoHL;
    public override uint ClassJobId => 13;
    public override Vector4 DefaultColor => new(0.70f, 0.55f, 0.30f, 1.0f);
}
