namespace DamageTerror.Jobs;

public sealed class LTW : JobDefinitionBase
{
    public override string Abbreviation => "Ltw";
    public override string FullName => "Leatherworker";
    public override JobRole Role => JobRole.DoHL;
    public override uint ClassJobId => 12;
    public override Vector4 DefaultColor => new(0.70f, 0.55f, 0.30f, 1.0f);
}
