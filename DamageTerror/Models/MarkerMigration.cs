namespace DamageTerror.Models;

internal static class MarkerMigration
{
    /// <summary>
    /// Routes legacy flat "Detail{Metric}Markers"/"GraphView{Metric}Markers" JSON keys
    /// captured via [JsonExtensionData] into the marker dictionaries, then clears the
    /// buffer so unknown keys are not re-serialized.
    /// </summary>
    public static void Apply(
        ref Dictionary<string, JToken>? extensionData,
        Dictionary<MetricType, SkillMarkerConfig> detailMarkers,
        Dictionary<MetricType, SkillMarkerConfig> graphViewMarkers)
    {
        if (extensionData == null || extensionData.Count == 0)
            return;

        foreach (var metric in new[] { MetricType.Dps, MetricType.Hps, MetricType.Dtps })
        {
            if (extensionData.TryGetValue($"Detail{metric}Markers", out var detail) && detail.Type != JTokenType.Null)
                detailMarkers[metric] = detail.ToObject<SkillMarkerConfig>() ?? new SkillMarkerConfig();
            if (extensionData.TryGetValue($"GraphView{metric}Markers", out var graph) && graph.Type != JTokenType.Null)
                graphViewMarkers[metric] = graph.ToObject<SkillMarkerConfig>() ?? new SkillMarkerConfig();
        }

        extensionData = null;
    }
}
