namespace DamageTerror.Jobs;

public abstract class JobDefinitionBase
{
    // ── Identity ──
    public abstract string Abbreviation { get; }
    public abstract string FullName { get; }
    public abstract JobRole Role { get; }
    public abstract uint ClassJobId { get; }
    public abstract Vector4 DefaultColor { get; }
    public virtual bool IsBaseClass => false;

    // ── DoT Potencies (statusId → tick potency) ──
    public virtual IReadOnlyDictionary<uint, int> DotTickPotencies { get; } = new Dictionary<uint, int>();

    // ── DoT Initial Hit Potencies (statusId → initial hit potency) ──
    public virtual IReadOnlyDictionary<uint, int> DotInitialHitPotencies { get; } = new Dictionary<uint, int>();

    // ── HoT Potencies (statusId → tick potency) ──
    public virtual IReadOnlyDictionary<uint, int> HotTickPotencies { get; } = new Dictionary<uint, int>();

    // ── Status IDs whose presence on a target reflects incoming damage back to the attacker ──
    public virtual IReadOnlySet<uint> KnownReflectStatusIds { get; } = new HashSet<uint>();

    // ── Ground-effect DoT IDs (statusId → skill name) ──
    public virtual IReadOnlyDictionary<uint, string> GroundEffectDots { get; } = new Dictionary<uint, string>();

    // ── Positional Fallback Data ──
    public virtual IReadOnlyList<PositionalFallbackEntry> FallbackPositionals { get; } = [];
}

public readonly record struct PositionalFallbackEntry(
    int ActionId,
    string ActionName,
    string Position,
    (int Percent, bool IsHit)[] Entries);
