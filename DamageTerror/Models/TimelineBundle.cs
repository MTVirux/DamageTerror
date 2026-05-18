using System.Runtime.Serialization;

namespace DamageTerror.Models;

/// <summary>
/// On-disk shape of a per-encounter timeline sidecar. Holds only the heavy
/// timestamped data streams — the encounter summary lives in encounters.json.
/// </summary>
public sealed class TimelineBundle
{
    public long EncounterId { get; set; }

    public Dictionary<string, List<GraphSample>> GraphData { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<SkillUseEvent>> SkillEvents { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<SkillUseEvent>> DamageTakenEvents { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<SkillUseEvent>> ItemEvents { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<StatusApplication>> StatusHistory { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<StatusApplication>> StatusesReceived { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context)
    {
        GraphData = EnsureCaseInsensitive(GraphData);
        SkillEvents = EnsureCaseInsensitive(SkillEvents);
        DamageTakenEvents = EnsureCaseInsensitive(DamageTakenEvents);
        ItemEvents = EnsureCaseInsensitive(ItemEvents);
        StatusHistory = EnsureCaseInsensitive(StatusHistory);
        StatusesReceived = EnsureCaseInsensitive(StatusesReceived);
    }

    private static Dictionary<string, List<TValue>> EnsureCaseInsensitive<TValue>(
        Dictionary<string, List<TValue>>? dict)
    {
        if (dict is null)
            return new Dictionary<string, List<TValue>>(StringComparer.OrdinalIgnoreCase);
        if (dict.Count > 0 && dict.Comparer != StringComparer.OrdinalIgnoreCase)
            return new Dictionary<string, List<TValue>>(dict, StringComparer.OrdinalIgnoreCase);
        return dict;
    }

    public static TimelineBundle FromSnapshot(EncounterSnapshot snapshot)
        => new()
        {
            EncounterId = snapshot.Id,
            GraphData = snapshot.GraphData,
            SkillEvents = snapshot.SkillEvents,
            DamageTakenEvents = snapshot.DamageTakenEvents,
            ItemEvents = snapshot.ItemEvents,
            StatusHistory = snapshot.StatusHistory,
            StatusesReceived = snapshot.StatusesReceived,
        };

    public void CopyInto(EncounterSnapshot snapshot)
    {
        snapshot.GraphData = GraphData;
        snapshot.SkillEvents = SkillEvents;
        snapshot.DamageTakenEvents = DamageTakenEvents;
        snapshot.ItemEvents = ItemEvents;
        snapshot.StatusHistory = StatusHistory;
        snapshot.StatusesReceived = StatusesReceived;
    }

    public bool IsEmpty =>
        GraphData.Count == 0
        && SkillEvents.Count == 0
        && DamageTakenEvents.Count == 0
        && ItemEvents.Count == 0
        && StatusHistory.Count == 0
        && StatusesReceived.Count == 0;
}
