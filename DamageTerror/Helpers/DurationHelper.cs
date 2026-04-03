namespace DamageTerror.Helpers;

internal static class DurationHelper
{
    internal static float ParseDuration(string? duration, float defaultValue = 0f)
    {
        if (string.IsNullOrEmpty(duration))
            return defaultValue;

        var parts = duration.Split(':');
        if (parts.Length == 2
            && float.TryParse(parts[0], out var mins)
            && float.TryParse(parts[1], out var secs))
            return Math.Max(defaultValue, mins * 60f + secs);

        if (parts.Length == 3
            && float.TryParse(parts[0], out var hrs)
            && float.TryParse(parts[1], out var m2)
            && float.TryParse(parts[2], out var s2))
            return Math.Max(defaultValue, hrs * 3600f + m2 * 60f + s2);

        return defaultValue;
    }
}
