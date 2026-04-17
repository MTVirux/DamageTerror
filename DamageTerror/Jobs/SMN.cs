namespace DamageTerror.Jobs;

public sealed class SMN : JobDefinitionBase
{
    public override string Abbreviation => "Smn";
    public override string FullName => "Summoner";
    public override JobRole Role => JobRole.CasterDps;
    public override uint ClassJobId => 27;
    public override Vector4 DefaultColor => new(0.1764706f, 0.60784316f, 0.47058824f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 2706, 30 },  // Slipstream
        { 3231, 65 },  // Scarlet Flame (PvP)
    };

    public override IReadOnlySet<uint> KnownDotStatusIds { get; } = new HashSet<uint>
    {
        2706, // Slipstream (Garuda)
        3231, // Scarlet Flame (PvP)
    };

    public override string MaxHitSkill => "Akh Morn";

    public override string[] DamageSkillNames =>
        ["Akh Morn", "Enkindle Bahamut", "Astral Impulse", "Ruby Rite", "Topaz Rite", "Emerald Rite"];

    public override (uint Id, string Name, float Duration, bool IsHoT)[] SampleBuffs =>
        [(1410u, "Searing Light", 30f, false)];

    public override (uint Id, string Name, float Duration, bool IsDot)[] SampleDebuffs => [];
}
