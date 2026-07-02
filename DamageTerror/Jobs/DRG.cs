namespace DamageTerror.Jobs;

public sealed class DRG : JobDefinitionBase
{
    public override string Abbreviation => "Drg";
    public override string FullName => "Dragoon";
    public override JobRole Role => JobRole.MeleeDps;
    public override uint ClassJobId => 22;
    public override Vector4 DefaultColor => new(0.25490198f, 0.39215687f, 0.8039216f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 118, 40 },   // Chaos Thrust
        { 2719, 45 },  // Chaotic Spring
    };

    public override IReadOnlyDictionary<uint, int> DotInitialHitPotencies { get; } = new Dictionary<uint, int>
    {
        { 118, 100 },  // Chaos Thrust
        { 2719, 300 }, // Chaotic Spring
    };

    public override IReadOnlyList<PositionalFallbackEntry> FallbackPositionals { get; } =
    [
        new(3554, "Fang and Claw", "Flank", [(0, false), (53, false), (10, true), (11, true), (58, true), (59, true)]),
        new(3556, "Wheeling Thrust", "Rear", [(0, false), (53, false), (10, true), (11, true), (58, true), (59, true)]),
        new(25772, "Chaotic Spring", "Rear", [(0, false), (53, false), (10, true), (11, true), (58, true), (59, true)]),
    ];

    public override string MaxHitSkill => "Stardiver";

    public override string[] DamageSkillNames =>
        ["Stardiver", "Nastrond", "Heaven's Thrust", "Chaotic Spring", "Wyrmwind Thrust", "Dragonfire Dive"];

    public override (uint Id, string Name, float Duration, bool IsHoT)[] SampleBuffs =>
        [(1210u, "Battle Litany", 20f, false), (1211u, "Dragon Sight", 20f, false)];

    public override (uint Id, string Name, float Duration, bool IsDot)[] SampleDebuffs =>
        [(2210u, "Chaotic Spring", 24f, true)];
}
