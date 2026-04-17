namespace DamageTerror.Jobs;

public sealed class LMB : JobDefinitionBase
{
    public override string Abbreviation => "Lmb";
    public override string FullName => "Limit Break";
    public override JobRole Role => JobRole.LimitBreak;
    public override uint ClassJobId => 0;
    public override Vector4 DefaultColor => new(0.5f, 0.5f, 0.5f, 1.0f);
}
