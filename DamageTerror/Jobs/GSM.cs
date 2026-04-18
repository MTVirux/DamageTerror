namespace DamageTerror.Jobs;

public sealed class GSM : JobDefinitionBase
{
    public override string Abbreviation => "Gsm";
    public override string FullName => "Goldsmith";
    public override JobRole Role => JobRole.DoHL;
    public override uint ClassJobId => 11;
    public override Vector4 DefaultColor => new(0.70f, 0.55f, 0.30f, 1.0f);
}
