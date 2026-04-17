namespace DamageTerror.Jobs;

public sealed class CNJ : JobDefinitionBase
{
    public override string Abbreviation => "Cnj";
    public override string FullName => "Conjurer";
    public override JobRole Role => JobRole.Healer;
    public override uint ClassJobId => 6;
    public override Vector4 DefaultColor => new(1.0f, 0.9411765f, 0.8627451f, 1.0f);
    public override bool IsBaseClass => true;
}
