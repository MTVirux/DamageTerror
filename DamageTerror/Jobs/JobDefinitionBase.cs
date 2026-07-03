namespace DamageTerror.Jobs;

public sealed class JobDefinitionBase
{
    private static readonly Dictionary<uint, int> EmptyPotencies = new();
    private static readonly Dictionary<uint, string> EmptyGroundEffects = new();
    private static readonly HashSet<uint> EmptyStatusIds = [];

    // ── Identity ──
    public string Abbreviation { get; }
    public string FullName { get; }
    public JobRole Role { get; }
    public uint ClassJobId { get; }
    public Vector4 DefaultColor { get; }
    public bool IsBaseClass { get; }

    // ── Potency / status data ──
    public IReadOnlyDictionary<uint, int> DotTickPotencies { get; }
    public IReadOnlyDictionary<uint, int> DotInitialHitPotencies { get; }
    public IReadOnlyDictionary<uint, int> HotTickPotencies { get; }
    public IReadOnlySet<uint> KnownReflectStatusIds { get; }
    public IReadOnlyDictionary<uint, string> GroundEffectDots { get; }
    public IReadOnlyList<PositionalFallbackEntry> FallbackPositionals { get; }

    public JobDefinitionBase(
        string abbreviation,
        string fullName,
        JobRole role,
        uint classJobId,
        Vector4 defaultColor,
        bool isBaseClass = false,
        Dictionary<uint, int>? dotTickPotencies = null,
        Dictionary<uint, int>? dotInitialHitPotencies = null,
        Dictionary<uint, int>? hotTickPotencies = null,
        HashSet<uint>? knownReflectStatusIds = null,
        Dictionary<uint, string>? groundEffectDots = null,
        IReadOnlyList<PositionalFallbackEntry>? fallbackPositionals = null)
    {
        Abbreviation = abbreviation;
        FullName = fullName;
        Role = role;
        ClassJobId = classJobId;
        DefaultColor = defaultColor;
        IsBaseClass = isBaseClass;

        DotTickPotencies = dotTickPotencies ?? EmptyPotencies;
        DotInitialHitPotencies = dotInitialHitPotencies ?? EmptyPotencies;
        HotTickPotencies = hotTickPotencies ?? EmptyPotencies;
        KnownReflectStatusIds = knownReflectStatusIds ?? EmptyStatusIds;
        GroundEffectDots = groundEffectDots ?? EmptyGroundEffects;
        FallbackPositionals = fallbackPositionals ?? [];
    }
}

public readonly record struct PositionalFallbackEntry(
    int ActionId,
    string ActionName,
    string Position,
    (int Percent, bool IsHit)[] Entries);
