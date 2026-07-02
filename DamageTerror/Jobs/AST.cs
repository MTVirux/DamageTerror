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
        { 838, 50 },   // Combust
        { 843, 60 },   // Combust II
        { 1881, 70 },  // Combust III
    };

    public override IReadOnlyDictionary<uint, int> HotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 835, 250 },  // Aspected Benefic
        { 836, 150 },  // Aspected Helios
        { 3894, 175 }, // Helios Conjunction
        { 848, 100 },  // Collective Unconscious
        { 956, 100 },  // Wheel of Fortune
    };

    public override IReadOnlyDictionary<uint, string> GroundEffectDots { get; } = new Dictionary<uint, string>
    {
        { 1122, "Earthly Star" },
    };
}
