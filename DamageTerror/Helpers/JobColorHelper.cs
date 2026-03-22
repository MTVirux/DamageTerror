namespace DamageTerror.Helpers;

/// <summary>
/// Maps FFXIV job roles to colors for the damage meter bars.
/// Supports config-overridden colors and hardcoded defaults.
/// </summary>
public static class JobColorHelper
{
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
    /// Returns the role for a given job abbreviation.
    /// </summary>
    public static JobRole GetRole(string job)
    {
        if (string.IsNullOrEmpty(job)) return JobRole.Default;
        if (Tanks.Contains(job)) return JobRole.Tank;
        if (Healers.Contains(job)) return JobRole.Healer;
        if (MeleeDps.Contains(job)) return JobRole.MeleeDps;
        if (RangedDps.Contains(job)) return JobRole.RangedDps;
        if (CasterDps.Contains(job)) return JobRole.CasterDps;
        return JobRole.Default;
    }

    /// <summary>
    /// Returns the role color for a given job, using config overrides.
    /// </summary>
    public static Vector4 GetColor(string job, Configuration config)
    {
        return GetRole(job) switch
        {
            JobRole.Tank => config.TankColor,
            JobRole.Healer => config.HealerColor,
            JobRole.MeleeDps => config.MeleeDpsColor,
            JobRole.RangedDps => config.RangedDpsColor,
            JobRole.CasterDps => config.CasterDpsColor,
            _ => config.DefaultJobColor,
        };
    }

    /// <summary>
    /// Returns a darkened version of the job color, suitable for bar fills.
    /// </summary>
    public static Vector4 GetBarColor(string job, float alpha, Configuration config)
    {
        var c = GetColor(job, config);
        return new Vector4(c.X * 0.8f, c.Y * 0.8f, c.Z * 0.8f, alpha);
    }
}

public enum JobRole
{
    Tank,
    Healer,
    MeleeDps,
    RangedDps,
    CasterDps,
    Default,
}
