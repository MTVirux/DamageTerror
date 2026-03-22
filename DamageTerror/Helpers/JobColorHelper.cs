namespace DamageTerror.Helpers;

/// <summary>
/// Maps FFXIV job roles to colors for the damage meter bars.
/// </summary>
public static class JobColorHelper
{
    // Role colors (RGBA as Vector4)
    private static readonly Vector4 TankColor = new(0.2f, 0.4f, 0.8f, 1.0f);       // Blue
    private static readonly Vector4 HealerColor = new(0.2f, 0.7f, 0.3f, 1.0f);     // Green
    private static readonly Vector4 MeleeDpsColor = new(0.8f, 0.2f, 0.2f, 1.0f);   // Red
    private static readonly Vector4 RangedDpsColor = new(0.9f, 0.5f, 0.2f, 1.0f);  // Orange
    private static readonly Vector4 CasterDpsColor = new(0.6f, 0.3f, 0.8f, 1.0f);  // Purple
    private static readonly Vector4 DefaultColor = new(0.5f, 0.5f, 0.5f, 1.0f);    // Grey

    // Tank jobs
    private static readonly HashSet<string> Tanks = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pld", "War", "Drk", "Gnb", "paladin", "warrior", "darkknight", "gunbreaker",
    };

    // Healer jobs
    private static readonly HashSet<string> Healers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Whm", "Sch", "Ast", "Sge", "whitemage", "scholar", "astrologian", "sage",
    };

    // Melee DPS jobs
    private static readonly HashSet<string> MeleeDps = new(StringComparer.OrdinalIgnoreCase)
    {
        "Mnk", "Drg", "Nin", "Sam", "Rpr", "Vpr",
        "monk", "dragoon", "ninja", "samurai", "reaper", "viper",
    };

    // Ranged Physical DPS jobs
    private static readonly HashSet<string> RangedDps = new(StringComparer.OrdinalIgnoreCase)
    {
        "Brd", "Mch", "Dnc", "bard", "machinist", "dancer",
    };

    // Caster DPS jobs
    private static readonly HashSet<string> CasterDps = new(StringComparer.OrdinalIgnoreCase)
    {
        "Blm", "Smn", "Rdm", "Pct", "Blu",
        "blackmage", "summoner", "redmage", "pictomancer", "bluemage",
    };

    /// <summary>
    /// Returns the role color for a given job abbreviation as used by ACT/IINACT.
    /// </summary>
    public static Vector4 GetColor(string job)
    {
        if (string.IsNullOrEmpty(job))
            return DefaultColor;

        if (Tanks.Contains(job)) return TankColor;
        if (Healers.Contains(job)) return HealerColor;
        if (MeleeDps.Contains(job)) return MeleeDpsColor;
        if (RangedDps.Contains(job)) return RangedDpsColor;
        if (CasterDps.Contains(job)) return CasterDpsColor;

        return DefaultColor;
    }

    /// <summary>
    /// Returns a darkened version of the job color, suitable for bar backgrounds.
    /// </summary>
    public static Vector4 GetBarColor(string job, float alpha)
    {
        var c = GetColor(job);
        return new Vector4(c.X * 0.8f, c.Y * 0.8f, c.Z * 0.8f, alpha);
    }
}
