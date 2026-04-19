namespace DamageTerror.Jobs;

public sealed class AST : JobDefinitionBase
{
    public override string Abbreviation => "Ast";
    public override string FullName => "Astrologian";
    public override JobRole Role => JobRole.Healer;
    public override uint ClassJobId => 33;
    public override Vector4 DefaultColor => new(1.0f, 0.90588236f, 0.2901961f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 838, 40 },   // Combust
        { 843, 50 },   // Combust II
        { 1881, 55 },  // Combust III
    };

    public override IReadOnlyDictionary<uint, int> HotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 835, 250 },  // Aspected Benefic
        { 836, 150 },  // Aspected Helios
        { 3894, 150 }, // Helios Conjunction
        { 848, 100 },  // Collective Unconscious
        { 956, 100 },  // Wheel of Fortune
    };

    public override IReadOnlySet<uint> KnownDotStatusIds { get; } = new HashSet<uint>
    {
        838,  // Combust
        843,  // Combust II
        1881, // Combust III
    };

    public override IReadOnlySet<uint> KnownHotStatusIds { get; } = new HashSet<uint>
    {
        835,  // Aspected Benefic
        836,  // Aspected Helios
        3894, // Helios Conjunction
        848,  // Collective Unconscious
        956,  // Wheel of Fortune
    };

    public override IReadOnlyDictionary<uint, string> GroundEffectDots { get; } = new Dictionary<uint, string>
    {
        { 1122, "Earthly Star" },
    };

    public override string MaxHitSkill => "Fall Malefic";

    public override string[] DamageSkillNames =>
        ["Fall Malefic", "Combust III", "Lord of Crowns", "Earthly Star", "Gravity II"];

    public override string[] HealSkillNames =>
        ["Aspected Benefic", "Aspected Helios", "Celestial Opposition", "Earthly Star", "Essential Dignity", "Macrocosmos"];

    public override (uint Id, string Name, float Duration, bool IsHoT)[] SampleBuffs =>
        [(1120u, "Aspected Benefic", 15f, true), (1121u, "Aspected Helios", 15f, true), (1122u, "Earthly Star", 20f, false), (1123u, "The Arrow", 15f, false), (1124u, "The Balance", 15f, false)];

    public override (uint Id, string Name, float Duration, bool IsDot)[] SampleDebuffs =>
        [(2120u, "Combust III", 30f, true)];
}
