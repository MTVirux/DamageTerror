namespace DamageTerror.Jobs;

public sealed class PCT : JobDefinitionBase
{
    public override string Abbreviation => "Pct";
    public override string FullName => "Pictomancer";
    public override JobRole Role => JobRole.CasterDps;
    public override uint ClassJobId => 42;
    public override Vector4 DefaultColor => new(0.9882353f, 0.57254905f, 0.88235295f, 1.0f);

    public override string MaxHitSkill => "Star Prism";

    public override string[] DamageSkillNames =>
        ["Star Prism", "Comet in Black", "Holy in White", "Fire in Red", "Creature Motif", "Hammer Stamp"];

    public override (uint Id, string Name, float Duration, bool IsHoT)[] SampleBuffs =>
        [(1430u, "Tempera Coat", 10f, false), (1431u, "Star Prism", 20f, false)];

    public override (uint Id, string Name, float Duration, bool IsDot)[] SampleDebuffs => [];
}
