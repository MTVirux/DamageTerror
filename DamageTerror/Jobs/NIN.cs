namespace DamageTerror.Jobs;

public sealed class NIN : JobDefinitionBase
{
    public override string Abbreviation => "Nin";
    public override string FullName => "Ninja";
    public override JobRole Role => JobRole.MeleeDps;
    public override uint ClassJobId => 30;
    public override Vector4 DefaultColor => new(0.6862745f, 0.09803922f, 0.39215687f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 501, 50 },   // Doton
        { 3184, 80 },  // Goka Mekkyaku (PvP)
        { 4304, 50 },  // Doton (PvP)
    };

    public override IReadOnlyDictionary<uint, int> HotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 3189, 65 },  // Meisui (PvP)
    };

    public override IReadOnlySet<uint> KnownDotStatusIds { get; } = new HashSet<uint>
    {
        3184, // Goka Mekkyaku (PvP)
    };

    public override IReadOnlySet<uint> KnownHotStatusIds { get; } = new HashSet<uint>
    {
        3189, // Meisui (PvP)
    };

    public override IReadOnlyDictionary<uint, string> GroundEffectDots { get; } = new Dictionary<uint, string>
    {
        { 501, "Doton" },   // PvE
        { 4304, "Doton" },  // PvP
    };

    public override IReadOnlyList<PositionalFallbackEntry> FallbackPositionals { get; } =
    [
        new(2255, "Aeolian Edge", "Rear", [(0, false), (47, false), (23, true), (30, true), (56, true), (59, true)]),
        new(2258, "Trick Attack", "Rear", [(0, false), (25, true)]),
        new(3563, "Armor Crush", "Flank", [(0, false), (47, false), (21, true), (27, true), (53, true), (58, true)]),
    ];

    public override string MaxHitSkill => "Hyosho Ranryu";

    public override string[] DamageSkillNames =>
        ["Hyosho Ranryu", "Forked Raijin", "Fleeting Raijin", "Bhavacakra", "Aeolian Edge", "Armor Crush"];

    public override (uint Id, string Name, float Duration, bool IsHoT)[] SampleBuffs =>
        [(1220u, "Trick Attack", 15f, false)];

    public override (uint Id, string Name, float Duration, bool IsDot)[] SampleDebuffs =>
        [(2220u, "Mug", 20f, false)];
}
