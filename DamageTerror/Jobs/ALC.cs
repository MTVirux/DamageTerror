namespace DamageTerror.Jobs;

public sealed class ALC : JobDefinitionBase
{
    public override string Abbreviation => "Alc";
    public override string FullName => "Alchemist";
    public override JobRole Role => JobRole.DoHL;
    public override uint ClassJobId => 14;
    public override Vector4 DefaultColor => new(0.70f, 0.55f, 0.30f, 1.0f);
}
