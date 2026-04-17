namespace DamageTerror.Jobs;

public sealed class PLD : JobDefinitionBase
{
    public override string Abbreviation => "Pld";
    public override string FullName => "Paladin";
    public override JobRole Role => JobRole.Tank;
    public override uint ClassJobId => 19;
    public override Vector4 DefaultColor => new(0.65882355f, 0.8235294f, 0.9019608f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 248, 30 },   // Circle of Scorn
    };

    public override IReadOnlyDictionary<uint, int> DotInitialHitPotencies { get; } = new Dictionary<uint, int>
    {
        { 248, 120 },  // Circle of Scorn
    };

    public override IReadOnlyDictionary<uint, int> HotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 2676, 250 }, // Knight's Benediction
    };

    public override IReadOnlySet<uint> KnownDotStatusIds { get; } = new HashSet<uint>
    {
        248,  // Circle of Scorn
    };

    public override IReadOnlySet<uint> KnownHotStatusIds { get; } = new HashSet<uint>
    {
        2676, // Knight's Benediction
    };

    public override string MaxHitSkill => "Confiteor";

    public override string[] DamageSkillNames =>
        ["Confiteor", "Blade of Honor", "Holy Spirit", "Atonement", "Goring Blade", "Royal Authority"];

    public override (uint Id, string Name, float Duration, bool IsHoT)[] SampleBuffs =>
        [(1010u, "Sentinel", 15f, false), (1011u, "Divine Veil", 30f, false), (1012u, "Hallowed Ground", 10f, false)];

    public override (uint Id, string Name, float Duration, bool IsDot)[] SampleDebuffs =>
        [(2010u, "Goring Blade", 21f, true)];
}
