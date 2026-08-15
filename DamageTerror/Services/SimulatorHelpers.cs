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

    /// <summary>Composite "skill-amount" string the meter splits back apart for the MaxHit / MaxHeal columns.</summary>
    public static string FormatMaxLabel(string skillName, long amount) => $"{skillName}-{amount}";

    public static double Percent(long part, long total) => total > 0 ? (double)part / total * 100.0 : 0.0;

    public static double Percent(int part, int total) => total > 0 ? (double)part / total * 100.0 : 0.0;

    /// <summary>Overheal as a share of raw healing output, matching how ACT reports OverHealPct.</summary>
    public static double OverhealPct(long healed, long overheal)
        => healed + overheal > 0 ? (double)overheal / (healed + overheal) * 100.0 : 0.0;

    /// <summary>Recomputes each entry's share of the combatant's total, mirroring the live
    /// skill list: sub-entries are measured against the same parent total.</summary>
    public static void RecomputeSkillPercents(List<SkillEntry> skills)
    {
        long total = 0;
        foreach (var s in skills) total += s.TotalDamage;
        if (total <= 0) return;

        foreach (var s in skills)
        {
            s.DamagePercent = (double)s.TotalDamage / total * 100.0;
            if (s.SubEntries == null) continue;
            foreach (var sub in s.SubEntries)
                sub.DamagePercent = (double)sub.TotalDamage / total * 100.0;
        }
    }
}
