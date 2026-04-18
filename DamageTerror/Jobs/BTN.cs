namespace DamageTerror.Jobs;

public sealed class BTN : JobDefinitionBase
{
    public override string Abbreviation => "Btn";
    public override string FullName => "Botanist";
    public override JobRole Role => JobRole.DoHL;
    public override uint ClassJobId => 17;
    public override Vector4 DefaultColor => new(0.70f, 0.55f, 0.30f, 1.0f);
}
