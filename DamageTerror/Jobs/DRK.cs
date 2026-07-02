namespace DamageTerror.Jobs;

public sealed class DRK : JobDefinitionBase
{
    public override string Abbreviation => "Drk";
    public override string FullName => "Dark Knight";
    public override JobRole Role => JobRole.Tank;
    public override uint ClassJobId => 32;
    public override Vector4 DefaultColor => new(0.81960785f, 0.14901961f, 0.8f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 749, 50 },   // Salted Earth
        { 3036, 80 },  // Salted Earth (PvP)
    };

    public override IReadOnlyDictionary<uint, int> HotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 3037, 80 },  // Salted Earth (PvP, self-HoT)
    };

    public override IReadOnlyDictionary<uint, string> GroundEffectDots { get; } = new Dictionary<uint, string>
    {
        { 749, "Salted Earth" },   // PvE
        { 3036, "Salted Earth" },  // PvP
    };

    public override string MaxHitSkill => "Living Shadow";

    public override string[] DamageSkillNames =>
        ["Living Shadow", "Shadowbringer", "Edge of Shadow", "Bloodspiller", "Carve and Spit", "Souleater"];

    public override (uint Id, string Name, float Duration, bool IsHoT)[] SampleBuffs =>
        [(1020u, "Shadow Wall", 15f, false), (1021u, "Dark Mind", 10f, false), (1022u, "The Blackest Night", 7f, false)];

    public override (uint Id, string Name, float Duration, bool IsDot)[] SampleDebuffs =>
        [(2020u, "Salted Earth", 15f, true)];
}
