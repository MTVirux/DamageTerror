namespace DamageTerror.Models;

/// <summary>
/// Represents an active status effect (buff/debuff) applied by a source to a target.
/// Tracked via ACT log line types 26 (GainsEffect) and 30 (LosesEffect).
/// </summary>
public struct ActiveStatus
{
    /// <summary>Player who applied this status.</summary>
    public string SourceName;

    /// <summary>Entity this status is applied to.</summary>
    public string TargetName;

    /// <summary>FFXIV status effect ID.</summary>
    public uint StatusId;

    /// <summary>Localized name of the status effect.</summary>
    public string StatusName;

    /// <summary>Encounter-relative time when the status was applied (seconds).</summary>
    public float AppliedAtSec;

    /// <summary>Duration in seconds as reported by the game.</summary>
    public float Duration;

    /// <summary>True if this status has an effectively infinite duration (>= 9999s).</summary>
    public bool IsPermanent;

    /// <summary>True if this status deals periodic damage (DoT).</summary>
    public bool IsDoT;

    /// <summary>True if this status applies periodic healing (HoT).</summary>
    public bool IsHoT;

    /// <summary>True if this is a buff (beneficial). False if debuff (detrimental).</summary>
    public bool IsBuff;

    /// <summary>Name of the action (type 21/22) that applied this status, if resolved.</summary>
    public string? ApplyingActionName;

    /// <summary>Encounter-relative time when this status was removed, if applicable (seconds).
    /// Set when the status is moved to the recently-removed buffer.</summary>
    public float RemovedAtSec;
}
