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
}
