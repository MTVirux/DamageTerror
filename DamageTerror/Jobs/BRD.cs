namespace DamageTerror.Jobs;

public sealed class BRD : JobDefinitionBase
{
    public override string Abbreviation => "Brd";
    public override string FullName => "Bard";
    public override JobRole Role => JobRole.RangedDps;
    public override uint ClassJobId => 23;
    public override Vector4 DefaultColor => new(0.5686275f, 0.7294118f, 0.36862746f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 124, 15 },   // Venomous Bite
        { 129, 20 },   // Windbite
        { 1200, 20 },  // Caustic Bite
        { 1201, 25 },  // Stormbite
    };

    public override IReadOnlyDictionary<uint, int> DotInitialHitPotencies { get; } = new Dictionary<uint, int>
    {
        { 1200, 150 }, // Caustic Bite
        { 1201, 100 }, // Stormbite
    };

    public override string MaxHitSkill => "Radiant Finale";

    public override string[] DamageSkillNames =>
        ["Radiant Finale", "Blast Arrow", "Apex Arrow", "Refulgent Arrow", "Burst Shot", "Iron Jaws"];

    public override (uint Id, string Name, float Duration, bool IsHoT)[] SampleBuffs =>
        [(1300u, "Mage's Ballad", 45f, false), (1301u, "Army's Paeon", 45f, false), (1302u, "The Wanderer's Minuet", 45f, false), (1303u, "Radiant Finale", 20f, false)];

    public override (uint Id, string Name, float Duration, bool IsDot)[] SampleDebuffs =>
        [(2300u, "Caustic Bite", 45f, true), (2301u, "Stormbite", 45f, true)];
}
