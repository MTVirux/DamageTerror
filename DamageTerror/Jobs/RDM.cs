namespace DamageTerror.Jobs;

public sealed class RDM : JobDefinitionBase
{
    public override string Abbreviation => "Rdm";
    public override string FullName => "Red Mage";
    public override JobRole Role => JobRole.CasterDps;
    public override uint ClassJobId => 35;
    public override Vector4 DefaultColor => new(0.9098039f, 0.48235294f, 0.48235294f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 4319, 65 },  // Scorch (PvP)
    };

    public override IReadOnlySet<uint> KnownDotStatusIds { get; } = new HashSet<uint>
    {
        4319, // Scorch (PvP)
    };

    public override string MaxHitSkill => "Scorch";

    public override string[] DamageSkillNames =>
        ["Scorch", "Resolution", "Verholy", "Verflare", "Fleche", "Contre Sixte"];

    public override (uint Id, string Name, float Duration, bool IsHoT)[] SampleBuffs =>
        [(1420u, "Embolden", 20f, false), (1421u, "Manafication", 10f, false)];

    public override (uint Id, string Name, float Duration, bool IsDot)[] SampleDebuffs => [];
}
