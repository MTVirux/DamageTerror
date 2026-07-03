namespace DamageTerror.Services;

internal static class SimulatorHelpers
{
    public static List<T> GetOrAdd<T>(this Dictionary<string, List<T>> dict, string key)
    {
        if (!dict.TryGetValue(key, out var list))
            dict[key] = list = new List<T>();
        return list;
    }

    public static string FormatDuration(float seconds)
    {
        var mins = (int)(seconds / 60f);
        var secs = (int)(seconds % 60f);
        return $"{mins:D2}:{secs:D2}";
    }

    public static string FormatPercent(long part, long total)
        => total > 0 ? $"{(double)part / total * 100:F1}%" : "0%";
}
