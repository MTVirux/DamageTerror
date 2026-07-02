namespace DamageTerror.Jobs;

public sealed class VPR : JobDefinitionBase
{
    public override string Abbreviation => "Vpr";
    public override string FullName => "Viper";
    public override JobRole Role => JobRole.MeleeDps;
    public override uint ClassJobId => 41;
    public override Vector4 DefaultColor => new(0.0627451f, 0.50980395f, 0.0627451f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 3667, 35 },  // Noxious Gnash
    };

    public override IReadOnlyDictionary<uint, int> DotInitialHitPotencies { get; } = new Dictionary<uint, int>
    {
        { 3667, 200 }, // Noxious Gnash
    };

    public override IReadOnlyList<PositionalFallbackEntry> FallbackPositionals { get; } =
    [
        new(34610, "Flanksting Strike", "Flank", [(0, false), (15, true), (12, true)]),
        new(34611, "Flanksbane Fang", "Flank", [(0, false), (15, true), (12, true)]),
        new(34612, "Hindsting Strike", "Rear", [(0, false), (15, true), (12, true)]),
        new(34613, "Hindsbane Fang", "Rear", [(0, false), (15, true), (12, true)]),
        new(34621, "Hunter's Coil", "Rear", [(0, false), (9, true)]),
        new(34622, "Swiftskin's Coil", "Flank", [(0, false), (9, true)]),
    ];

    public override string MaxHitSkill => "Ouroboros";

    public override string[] DamageSkillNames =>
        ["Ouroboros", "Reawaken", "Uncoiled Fury", "Hindsting Strike", "Flanksbane Fang", "Hunter's Sting"];

    public override (uint Id, string Name, float Duration, bool IsHoT)[] SampleBuffs =>
        [(1250u, "Serpent's Ire", 15f, false)];

    public override (uint Id, string Name, float Duration, bool IsDot)[] SampleDebuffs =>
        [(2250u, "Noxious Gnash", 20f, true)];
}
