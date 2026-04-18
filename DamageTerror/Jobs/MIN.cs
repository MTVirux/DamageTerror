namespace DamageTerror.Jobs;

public sealed class MIN : JobDefinitionBase
{
    public override string Abbreviation => "Min";
    public override string FullName => "Miner";
    public override JobRole Role => JobRole.DoHL;
    public override uint ClassJobId => 16;
    public override Vector4 DefaultColor => new(0.70f, 0.55f, 0.30f, 1.0f);
}
