namespace DamageTerror.Jobs;

public sealed class CUL : JobDefinitionBase
{
    public override string Abbreviation => "Cul";
    public override string FullName => "Culinarian";
    public override JobRole Role => JobRole.DoHL;
    public override uint ClassJobId => 15;
    public override Vector4 DefaultColor => new(0.70f, 0.55f, 0.30f, 1.0f);
}
