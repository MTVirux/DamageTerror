namespace DamageTerror.Jobs;

public sealed class MRD : JobDefinitionBase
{
    public override string Abbreviation => "Mrd";
    public override string FullName => "Marauder";
    public override JobRole Role => JobRole.Tank;
    public override uint ClassJobId => 3;
    public override Vector4 DefaultColor => new(0.8117647f, 0.14901961f, 0.12941177f, 1.0f);
    public override bool IsBaseClass => true;
}
