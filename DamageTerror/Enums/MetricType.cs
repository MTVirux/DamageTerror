namespace DamageTerror.Enums;

/// <summary>
/// Identifies a per-metric graph series (damage / healing / damage taken
/// per second). Used as the key for marker-config dictionaries on
/// Configuration and ThemePreset. Values are persisted in JSON (as the
/// keys of Dictionary&lt;MetricType, SkillMarkerConfig&gt; objects), so
/// **never rename these values** — doing so would break user data.
/// </summary>
public enum MetricType
{
    Dps,
    Hps,
    Dtps,
}
