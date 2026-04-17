namespace DamageTerror.Jobs;

public sealed class ARC : JobDefinitionBase
{
    public override string Abbreviation => "Arc";
    public override string FullName => "Archer";
    public override JobRole Role => JobRole.RangedDps;
    public override uint ClassJobId => 5;
    public override Vector4 DefaultColor => new(0.5686275f, 0.7294118f, 0.36862746f, 1.0f);
    public override bool IsBaseClass => true;
}
