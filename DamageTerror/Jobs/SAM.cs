namespace DamageTerror.Jobs;

public sealed class SAM : JobDefinitionBase
{
    public override string Abbreviation => "Sam";
    public override string FullName => "Samurai";
    public override JobRole Role => JobRole.MeleeDps;
    public override uint ClassJobId => 34;
    public override Vector4 DefaultColor => new(0.89411765f, 0.42745098f, 0.015686275f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 1228, 45 },  // Higanbana
    };

    public override IReadOnlyDictionary<uint, int> DotInitialHitPotencies { get; } = new Dictionary<uint, int>
    {
        { 1228, 200 }, // Higanbana
    };

    public override IReadOnlyList<PositionalFallbackEntry> FallbackPositionals { get; } =
    [
        new(7481, "Gekko", "Rear", [(0, false), (53, false), (10, true), (22, true), (11, true), (58, true)]),
        new(7482, "Kasha", "Flank", [(0, false), (53, false), (10, true), (22, true), (11, true), (58, true)]),
    ];

    public override string MaxHitSkill => "Midare Setsugekka";

    public override string[] DamageSkillNames =>
        ["Midare Setsugekka", "Ogi Namikiri", "Kaeshi: Namikiri", "Higanbana", "Shinten", "Shoha"];

    public override (uint Id, string Name, float Duration, bool IsHoT)[] SampleBuffs =>
        [(1230u, "Meikyo Shisui", 15f, false)];

    public override (uint Id, string Name, float Duration, bool IsDot)[] SampleDebuffs =>
        [(2230u, "Higanbana", 60f, true)];
}
