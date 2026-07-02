namespace DamageTerror.Jobs;

public sealed class SGE : JobDefinitionBase
{
    public override string Abbreviation => "Sge";
    public override string FullName => "Sage";
    public override JobRole Role => JobRole.Healer;
    public override uint ClassJobId => 40;
    public override Vector4 DefaultColor => new(0.5019608f, 0.627451f, 0.9411765f, 1.0f);

    public override IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 2614, 40 },  // Eukrasian Dosis
        { 2615, 60 },  // Eukrasian Dosis II
        { 2616, 90 },  // Eukrasian Dosis III
        { 3897, 40 },  // Eukrasian Dyskrasia
        { 3976, 50 },  // Eukrasian Dosis III (PvP)
    };

    public override IReadOnlyDictionary<uint, int> HotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 2617, 100 }, // Physis
        { 2620, 100 }, // Physis II
        { 2938, 100 }, // Kerakeia
        { 3898, 170 }, // Philosophia
    };

    public override string MaxHitSkill => "Pneuma";

    public override string[] DamageSkillNames =>
        ["Dosis III", "Eukrasian Dosis III", "Phlegma III", "Toxikon II", "Pneuma"];

    public override string[] HealSkillNames =>
        ["Eukrasian Diagnosis", "Eukrasian Prognosis", "Druochole", "Kerachole", "Ixochole", "Pneuma"];

    public override (uint Id, string Name, float Duration, bool IsHoT)[] SampleBuffs =>
        [(1130u, "Eukrasian Diagnosis", 30f, false), (1131u, "Kerachole", 15f, true), (1132u, "Holos", 20f, false), (1133u, "Physis II", 15f, true)];

    public override (uint Id, string Name, float Duration, bool IsDot)[] SampleDebuffs =>
        [(2130u, "Eukrasian Dosis III", 30f, true)];
}
