namespace DamageTerror.Models;

/// <summary>
/// Represents an active status effect (buff/debuff) applied by a source to a target.
/// Tracked via ACT log line types 26 (GainsEffect) and 30 (LosesEffect).
/// </summary>
public struct ActiveStatus
{
    public string SourceName;
    public string TargetName;
    public uint StatusId;
    public string StatusName;

    /// <summary>Encounter-relative time (seconds).</summary>
    public float AppliedAtSec;

    public float Duration;

    /// <summary>True if duration >= 9999s.</summary>
    public bool IsPermanent;

    public bool IsDoT;
    public bool IsHoT;
    public bool IsBuff;

    /// <summary>Name of the action (type 21/22) that applied this status, if resolved.</summary>
    public string? ApplyingActionName;

    /// <summary>Encounter-relative time when moved to the recently-removed buffer.</summary>
    public float RemovedAtSec;

    /// <summary>LSB of expected non-crit tick damage from the 0x0E status-application effect.</summary>
    public byte DamageLowByte;

    /// <summary>Crit rate × 10 from 0x0E effect flags (200 = 20.0%). Overflows at 25.6%.</summary>
    public byte CritLowByte;

    /// <summary>True if DamageLowByte/CritLowByte were populated from a 0x0E effect pair.</summary>
    public bool HasLowByteData;
}
