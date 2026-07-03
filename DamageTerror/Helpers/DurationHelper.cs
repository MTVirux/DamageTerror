namespace DamageTerror.Helpers;

internal static class DurationHelper
{
    internal static float ParseDuration(string? duration, float defaultValue = 0f)
    {
        if (string.IsNullOrEmpty(duration))
            return defaultValue;

        var parts = duration.Split(':');
        if (parts.Length is < 2 or > 3)
            return defaultValue;

        var total = 0f;
        foreach (var part in parts)
        {
            if (!float.TryParse(part, out var value))
                return defaultValue;
            total = total * 60f + value;
        }

        return Math.Max(defaultValue, total);
    }
}
