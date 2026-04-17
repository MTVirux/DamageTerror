namespace DamageTerror.Models;

public sealed class StatusApplication
{
    public uint StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public float AppliedAtSec { get; set; }
    public float Duration { get; set; }
    public float? RemovedAtSec { get; set; }
    public bool IsPermanent { get; set; }
    public bool IsDoT { get; set; }
    public bool IsHoT { get; set; }
    public bool IsBuff { get; set; }
}
