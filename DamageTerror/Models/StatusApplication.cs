namespace DamageTerror.Models;

/// <summary>
/// Record of a single status application for uptime tracking.
/// </summary>
public class StatusApplication
{
    public uint StatusId;
    public string StatusName = string.Empty;
    public string SourceName = string.Empty;
    public string TargetName = string.Empty;
    public float AppliedAtSec;
    public float Duration;
    public float? RemovedAtSec;
    public bool IsPermanent;
    public bool IsDoT;
    public bool IsHoT;
    public bool IsBuff;
}
