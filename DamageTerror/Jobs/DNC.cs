namespace DamageTerror.Jobs;

public sealed class DNC : JobDefinitionBase
{
    public override string Abbreviation => "Dnc";
    public override string FullName => "Dancer";
    public override JobRole Role => JobRole.RangedDps;
    public override uint ClassJobId => 38;
    public override Vector4 DefaultColor => new(0.8862745f, 0.6901961f, 0.6862745f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 3162, 75 },  // Honing Dance (PvP)
    };

    public override IReadOnlyDictionary<uint, int> HotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 2695, 100 }, // Improvisation
    };

    public override IReadOnlySet<uint> KnownHotStatusIds { get; } = new HashSet<uint>
    {
        2695, // Improvisation
    };

    public override IReadOnlyDictionary<uint, string> GroundEffectDots { get; } = new Dictionary<uint, string>
    {
        { 3162, "Honing Dance" }, // PvP
    };

    public override string MaxHitSkill => "Technical Finish";

    public override string[] DamageSkillNames =>
        ["Technical Finish", "Starfall Dance", "Saber Dance", "Tillana", "Standard Finish", "Fan Dance IV"];

    public override (uint Id, string Name, float Duration, bool IsHoT)[] SampleBuffs =>
        [(1320u, "Technical Finish", 20f, false), (1321u, "Standard Finish", 60f, false), (1322u, "Devilment", 20f, false)];

    public override (uint Id, string Name, float Duration, bool IsDot)[] SampleDebuffs =>
        [(2320u, "Closed Position", 60f, false)];
}
