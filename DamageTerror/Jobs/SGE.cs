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
}
