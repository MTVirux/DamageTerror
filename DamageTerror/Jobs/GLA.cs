namespace DamageTerror.Jobs;

public sealed class GLA : JobDefinitionBase
{
    public override string Abbreviation => "Gla";
    public override string FullName => "Gladiator";
    public override JobRole Role => JobRole.Tank;
    public override uint ClassJobId => 1;
    public override Vector4 DefaultColor => new(0.65882355f, 0.8235294f, 0.9019608f, 1.0f);
    public override bool IsBaseClass => true;
}
