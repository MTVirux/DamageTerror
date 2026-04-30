namespace DamageTerror.Jobs;

public sealed class GNB : JobDefinitionBase
{
    public override string Abbreviation => "Gnb";
    public override string FullName => "Gunbreaker";
    public override JobRole Role => JobRole.Tank;
    public override uint ClassJobId => 37;
    public override Vector4 DefaultColor => new(0.47450981f, 0.42745098f, 0.18823530f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 1837, 120 },  // Sonic Break
        { 1838, 60 },  // Bow Shock
    };

    public override IReadOnlyDictionary<uint, int> DotInitialHitPotencies { get; } = new Dictionary<uint, int>
    {
        { 1837, 340 }, // Sonic Break
        { 1838, 150 }, // Bow Shock
    };

    public override IReadOnlyDictionary<uint, int> HotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 1835, 200 }, // Aurora
    };

    public override IReadOnlySet<uint> KnownDotStatusIds { get; } = new HashSet<uint>
    {
        1837, // Sonic Break
        1838, // Bow Shock
    };

    public override IReadOnlySet<uint> KnownHotStatusIds { get; } = new HashSet<uint>
    {
        1835, // Aurora
    };

    public override string MaxHitSkill => "Blasting Zone";

    public override string[] DamageSkillNames =>
        ["Blasting Zone", "Double Down", "Sonic Break", "Burst Strike", "Gnashing Fang", "Hypervelocity"];

    public override (uint Id, string Name, float Duration, bool IsHoT)[] SampleBuffs =>
        [(1030u, "Nebula", 15f, false), (1031u, "Camouflage", 20f, false), (1032u, "Heart of Corundum", 8f, false)];

    public override (uint Id, string Name, float Duration, bool IsDot)[] SampleDebuffs =>
        [(2030u, "Sonic Break", 30f, true)];
}
