namespace DamageTerror.Models;

/// <summary>
/// On-disk shape of a per-encounter raw capture sidecar. Holds only the debug-only
/// replay data - the encounter summary lives in encounters.json.
/// </summary>
public sealed class RawCaptureBundle
{
    public long EncounterId { get; set; }

    public List<string> RawLogLines { get; set; } = new();
    public List<string> RawCombatDataFrames { get; set; } = new();

    public static RawCaptureBundle FromSnapshot(EncounterSnapshot snapshot)
        => new()
        {
            EncounterId = snapshot.Id,
            RawLogLines = snapshot.RawLogLines,
            RawCombatDataFrames = snapshot.RawCombatDataFrames,
        };

    public void CopyInto(EncounterSnapshot snapshot)
    {
        snapshot.RawLogLines = RawLogLines;
        snapshot.RawCombatDataFrames = RawCombatDataFrames;
    }

    [JsonIgnore]
    public bool IsEmpty => RawLogLines.Count == 0 && RawCombatDataFrames.Count == 0;
}
