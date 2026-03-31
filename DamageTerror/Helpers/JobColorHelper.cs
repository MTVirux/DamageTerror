namespace DamageTerror.Helpers;

public static class JobColorHelper
{
    private static readonly HashSet<string> Tanks = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pld", "War", "Drk", "Gnb", "paladin", "warrior", "darkknight", "gunbreaker",
    };

    private static readonly HashSet<string> Healers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Whm", "Sch", "Ast", "Sge", "whitemage", "scholar", "astrologian", "sage",
    };

    private static readonly HashSet<string> MeleeDps = new(StringComparer.OrdinalIgnoreCase)
    {
        "Mnk", "Drg", "Nin", "Sam", "Rpr", "Vpr",
        "monk", "dragoon", "ninja", "samurai", "reaper", "viper",
    };

    private static readonly HashSet<string> RangedDps = new(StringComparer.OrdinalIgnoreCase)
    {
        "Brd", "Mch", "Dnc", "bard", "machinist", "dancer",
    };

    private static readonly HashSet<string> CasterDps = new(StringComparer.OrdinalIgnoreCase)
    {
        "Blm", "Smn", "Rdm", "Pct", "Blu",
        "blackmage", "summoner", "redmage", "pictomancer", "bluemage",
    };

    private static readonly HashSet<string> LimitBreak = new(StringComparer.OrdinalIgnoreCase)
    {
        "Lmb", "Limit Break",
    };

    public static readonly string[] AllJobAbbreviations =
    {
        "Pld", "War", "Drk", "Gnb",
        "Whm", "Sch", "Ast", "Sge",
        "Mnk", "Drg", "Nin", "Sam", "Rpr", "Vpr",
        "Brd", "Mch", "Dnc",
        "Blm", "Smn", "Rdm", "Pct", "Blu",
    };

    private static readonly Dictionary<string, Vector4> DefaultPerJobColors = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Pld", new Vector4(0.40f, 0.55f, 0.90f, 1.0f) },
        { "War", new Vector4(0.20f, 0.30f, 0.70f, 1.0f) },
        { "Drk", new Vector4(0.50f, 0.20f, 0.60f, 1.0f) },
        { "Gnb", new Vector4(0.25f, 0.45f, 0.65f, 1.0f) },


        { "Whm", new Vector4(0.85f, 0.85f, 0.70f, 1.0f) },
        { "Sch", new Vector4(0.30f, 0.45f, 0.85f, 1.0f) },
        { "Ast", new Vector4(0.90f, 0.75f, 0.30f, 1.0f) },
        { "Sge", new Vector4(0.35f, 0.65f, 0.75f, 1.0f) },


        { "Mnk", new Vector4(0.85f, 0.65f, 0.15f, 1.0f) },
        { "Drg", new Vector4(0.25f, 0.40f, 0.85f, 1.0f) },
        { "Nin", new Vector4(0.70f, 0.20f, 0.35f, 1.0f) },
        { "Sam", new Vector4(0.90f, 0.55f, 0.20f, 1.0f) },
        { "Rpr", new Vector4(0.60f, 0.25f, 0.40f, 1.0f) },
        { "Vpr", new Vector4(0.45f, 0.70f, 0.30f, 1.0f) },


        { "Brd", new Vector4(0.55f, 0.80f, 0.30f, 1.0f) },
        { "Mch", new Vector4(0.45f, 0.75f, 0.80f, 1.0f) },
        { "Dnc", new Vector4(0.85f, 0.55f, 0.65f, 1.0f) },


        { "Blm", new Vector4(0.60f, 0.45f, 0.85f, 1.0f) },
        { "Smn", new Vector4(0.30f, 0.70f, 0.40f, 1.0f) },
        { "Rdm", new Vector4(0.85f, 0.35f, 0.45f, 1.0f) },
        { "Pct", new Vector4(0.75f, 0.55f, 0.80f, 1.0f) },
        { "Blu", new Vector4(0.30f, 0.55f, 0.90f, 1.0f) },
    };

    public static Vector4 GetDefaultJobColor(string job)
    {
        if (!string.IsNullOrEmpty(job) && DefaultPerJobColors.TryGetValue(job, out var c))
            return c;
        return new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
    }

    public static JobRole GetRole(string job)
    {
        if (string.IsNullOrEmpty(job)) return JobRole.Default;
        if (Tanks.Contains(job)) return JobRole.Tank;
        if (Healers.Contains(job)) return JobRole.Healer;
        if (MeleeDps.Contains(job)) return JobRole.MeleeDps;
        if (RangedDps.Contains(job)) return JobRole.RangedDps;
        if (CasterDps.Contains(job)) return JobRole.CasterDps;
        if (LimitBreak.Contains(job)) return JobRole.LimitBreak;
        return JobRole.Default;
    }

    public static Vector4 GetColor(string job, Configuration config)
    {
        if (config.UsePerJobColors && !string.IsNullOrEmpty(job))
        {
            if (config.JobColors.TryGetValue(job, out var custom))
                return custom;

            if (DefaultPerJobColors.TryGetValue(job, out var def))
                return def;
        }

        return GetRole(job) switch
        {
            JobRole.Tank => config.TankColor,
            JobRole.Healer => config.HealerColor,
            JobRole.MeleeDps => config.MeleeDpsColor,
            JobRole.RangedDps => config.RangedDpsColor,
            JobRole.CasterDps => config.CasterDpsColor,
            JobRole.LimitBreak => config.LimitBreakColor,
            _ => config.DefaultJobColor,
        };
    }

    public static Vector4 GetBarColor(string job, float alpha, Configuration config)
    {
        var c = GetColor(job, config);
        return new Vector4(c.X * 0.8f, c.Y * 0.8f, c.Z * 0.8f, alpha);
    }
}
