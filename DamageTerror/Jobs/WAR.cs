namespace DamageTerror.Jobs;

public sealed class WAR : JobDefinitionBase
{
    public override string Abbreviation => "War";
    public override string FullName => "Warrior";
    public override JobRole Role => JobRole.Tank;
    public override uint ClassJobId => 21;
    public override Vector4 DefaultColor => new(0.8117647f, 0.14901961f, 0.12941177f, 1.0f);

    public override IReadOnlyDictionary<uint, int> HotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 2681, 200 }, // Equilibrium
        { 2108, 100 }, // Shake It Off (Over Time)
    };

    public override IReadOnlySet<uint> KnownHotStatusIds { get; } = new HashSet<uint>
    {
        2681, // Equilibrium
        2108, // Shake It Off (Over Time)
    };

    public override IReadOnlySet<uint> KnownReflectStatusIds { get; } = new HashSet<uint>
    {
        89,   // Vengeance
        3832, // Damnation
    };

    public override string MaxHitSkill => "Primal Rend";

    public override string[] DamageSkillNames =>
        ["Primal Rend", "Inner Chaos", "Fell Cleave", "Upheaval", "Onslaught", "Storm's Eye"];

    public override (uint Id, string Name, float Duration, bool IsHoT)[] SampleBuffs =>
        [(1001u, "Vengeance", 15f, false), (1002u, "Thrill of Battle", 10f, false), (1003u, "Shake It Off", 30f, false)];

    public override (uint Id, string Name, float Duration, bool IsDot)[] SampleDebuffs =>
        [(2001u, "Storm's Eye", 30f, false)];
}
