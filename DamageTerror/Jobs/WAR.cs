namespace DamageTerror.Jobs;

public sealed class WAR : JobDefinitionBase
{
    public override string Abbreviation => "War";
    public override string FullName => "Warrior";
    public override JobRole Role => JobRole.Tank;
    public override uint ClassJobId => 21;
    public override Vector4 DefaultColor => new(0.8117647f, 0.14901961f, 0.12941177f, 1.0f);

    public override IReadOnlyDictionary<uint, int> HotTickPotencies { get; } = new Dictionary<uint, int>
    {
        { 2681, 200 }, // Equilibrium
        { 2108, 100 }, // Shake It Off (Over Time)
    };

    public override IReadOnlySet<uint> KnownReflectStatusIds { get; } = new HashSet<uint>
    {
        89,   // Vengeance
        3832, // Damnation
    };
}
