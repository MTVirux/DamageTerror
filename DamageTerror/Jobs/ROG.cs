namespace DamageTerror.Jobs;

public sealed class ROG : JobDefinitionBase
{
    public override string Abbreviation => "Rog";
    public override string FullName => "Rogue";
    public override JobRole Role => JobRole.MeleeDps;
    public override uint ClassJobId => 29;
    public override Vector4 DefaultColor => new(0.6862745f, 0.09803922f, 0.39215687f, 1.0f);
    public override bool IsBaseClass => true;
}
