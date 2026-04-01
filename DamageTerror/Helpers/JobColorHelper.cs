namespace DamageTerror.Helpers;

public static class JobColorHelper
{
    public static readonly string[] TankJobs = { "Pld", "War", "Drk", "Gnb" };
    public static readonly string[] HealerJobs = { "Whm", "Sch", "Ast", "Sge" };
    public static readonly string[] MeleeDpsJobs = { "Mnk", "Drg", "Nin", "Sam", "Rpr", "Vpr" };
    public static readonly string[] RangedDpsJobs = { "Brd", "Mch", "Dnc" };
    public static readonly string[] CasterDpsJobs = { "Blm", "Smn", "Rdm", "Pct", "Blu" };
    public static readonly string[] BaseClassJobs = { "Gla", "Mrd", "Cnj", "Pgl", "Lnc", "Arc", "Rog", "Thm", "Acn" };

    public static readonly string[] AllJobAbbreviations =
        TankJobs.Concat(HealerJobs).Concat(MeleeDpsJobs).Concat(RangedDpsJobs).Concat(CasterDpsJobs).Concat(BaseClassJobs).ToArray();

    private static readonly string[] TankFullNames = { "paladin", "warrior", "darkknight", "gunbreaker", "gladiator", "marauder" };
    private static readonly string[] HealerFullNames = { "whitemage", "scholar", "astrologian", "sage", "conjurer" };
    private static readonly string[] MeleeDpsFullNames = { "monk", "dragoon", "ninja", "samurai", "reaper", "viper", "pugilist", "lancer", "rogue" };
    private static readonly string[] RangedDpsFullNames = { "bard", "machinist", "dancer", "archer" };
    private static readonly string[] CasterDpsFullNames = { "blackmage", "summoner", "redmage", "pictomancer", "bluemage", "thaumaturge", "arcanist" };

    private static HashSet<string> BuildRoleSet(string[] abbreviations, string[] fullNames) =>
        new(abbreviations.Concat(fullNames), StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> Tanks = BuildRoleSet(TankJobs.Concat(new[] { "Gla", "Mrd" }).ToArray(), TankFullNames);
    private static readonly HashSet<string> Healers = BuildRoleSet(HealerJobs.Concat(new[] { "Cnj" }).ToArray(), HealerFullNames);
    private static readonly HashSet<string> MeleeDps = BuildRoleSet(MeleeDpsJobs.Concat(new[] { "Pgl", "Lnc", "Rog" }).ToArray(), MeleeDpsFullNames);
    private static readonly HashSet<string> RangedDps = BuildRoleSet(RangedDpsJobs.Concat(new[] { "Arc" }).ToArray(), RangedDpsFullNames);
    private static readonly HashSet<string> CasterDps = BuildRoleSet(CasterDpsJobs.Concat(new[] { "Thm", "Acn" }).ToArray(), CasterDpsFullNames);
    private static readonly HashSet<string> LimitBreak = new(StringComparer.OrdinalIgnoreCase) { "Lmb", "Limit Break" };

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

        { "Gla", new Vector4(0.40f, 0.55f, 0.90f, 1.0f) },
        { "Mrd", new Vector4(0.20f, 0.30f, 0.70f, 1.0f) },
        { "Cnj", new Vector4(0.85f, 0.85f, 0.70f, 1.0f) },
        { "Pgl", new Vector4(0.85f, 0.65f, 0.15f, 1.0f) },
        { "Lnc", new Vector4(0.25f, 0.40f, 0.85f, 1.0f) },
        { "Arc", new Vector4(0.55f, 0.80f, 0.30f, 1.0f) },
        { "Rog", new Vector4(0.70f, 0.20f, 0.35f, 1.0f) },
        { "Thm", new Vector4(0.60f, 0.45f, 0.85f, 1.0f) },
        { "Acn", new Vector4(0.30f, 0.70f, 0.40f, 1.0f) }
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
