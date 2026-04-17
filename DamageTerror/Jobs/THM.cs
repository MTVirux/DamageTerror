namespace DamageTerror.Jobs;

public sealed class THM : JobDefinitionBase
{
    public override string Abbreviation => "Thm";
    public override string FullName => "Thaumaturge";
    public override JobRole Role => JobRole.CasterDps;
    public override uint ClassJobId => 7;
    public override Vector4 DefaultColor => new(0.64705884f, 0.4745098f, 0.8392157f, 1.0f);
    public override bool IsBaseClass => true;
}
