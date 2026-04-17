namespace DamageTerror.Jobs;

public sealed class LNC : JobDefinitionBase
{
    public override string Abbreviation => "Lnc";
    public override string FullName => "Lancer";
    public override JobRole Role => JobRole.MeleeDps;
    public override uint ClassJobId => 4;
    public override Vector4 DefaultColor => new(0.25490198f, 0.39215687f, 0.8039216f, 1.0f);
    public override bool IsBaseClass => true;
}
