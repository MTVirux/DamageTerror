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
        { "Pld", new Vector4(0.65882355f, 0.8235294f, 0.9019608f, 1.0f) },
        { "War", new Vector4(0.8117647f, 0.14901961f, 0.12941177f, 1.0f) },
        { "Drk", new Vector4(0.81960785f, 0.14901961f, 0.8f, 1.0f) },
        { "Gnb", new Vector4(0.47450981f, 0.42745098f, 0.18823530f, 1.0f) },

        { "Whm", new Vector4(1.0f, 0.9411765f, 0.8627451f, 1.0f) },
        { "Sch", new Vector4(0.5254902f, 0.34117648f, 1.0f, 1.0f) },
        { "Ast", new Vector4(1.0f, 0.90588236f, 0.2901961f, 1.0f) },
        { "Sge", new Vector4(0.65882355f, 0.8235294f, 0.9019608f, 1.0f) },

        { "Mnk", new Vector4(0.8392157f, 0.6117647f, 0.0f, 1.0f) },
        { "Drg", new Vector4(0.25490198f, 0.39215687f, 0.8039216f, 1.0f) },
        { "Nin", new Vector4(0.6862745f, 0.09803922f, 0.39215687f, 1.0f) },
        { "Sam", new Vector4(0.89411765f, 0.42745098f, 0.015686275f, 1.0f) },
        { "Rpr", new Vector4(0.5882353f, 0.3529412f, 0.5647059f, 1.0f) },
        { "Vpr", new Vector4(0.0627451f, 0.50980395f, 0.0627451f, 1.0f) },

        { "Brd", new Vector4(0.5686275f, 0.7294118f, 0.36862746f, 1.0f) },
        { "Mch", new Vector4(0.43137255f, 0.88235295f, 0.8392157f, 1.0f) },
        { "Dnc", new Vector4(0.8862745f, 0.6901961f, 0.6862745f, 1.0f) },

        { "Blm", new Vector4(0.64705884f, 0.4745098f, 0.8392157f, 1.0f) },
        { "Smn", new Vector4(0.1764706f, 0.60784316f, 0.47058824f, 1.0f) },
        { "Rdm", new Vector4(0.9098039f, 0.48235294f, 0.48235294f, 1.0f) },
        { "Pct", new Vector4(0.9882353f, 0.57254905f, 0.88235295f, 1.0f) },
        { "Blu", new Vector4(0.30f, 0.55f, 0.90f, 1.0f) },

        { "Gla", new Vector4(0.65882355f, 0.8235294f, 0.9019608f, 1.0f) },
        { "Mrd", new Vector4(0.8117647f, 0.14901961f, 0.12941177f, 1.0f) },
        { "Cnj", new Vector4(1.0f, 0.9411765f, 0.8627451f, 1.0f) },
        { "Pgl", new Vector4(0.8392157f, 0.6117647f, 0.0f, 1.0f) },
        { "Lnc", new Vector4(0.25490198f, 0.39215687f, 0.8039216f, 1.0f) },
        { "Arc", new Vector4(0.5686275f, 0.7294118f, 0.36862746f, 1.0f) },
        { "Rog", new Vector4(0.6862745f, 0.09803922f, 0.39215687f, 1.0f) },
        { "Thm", new Vector4(0.64705884f, 0.4745098f, 0.8392157f, 1.0f) },
        { "Acn", new Vector4(0.1764706f, 0.60784316f, 0.47058824f, 1.0f) }
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
