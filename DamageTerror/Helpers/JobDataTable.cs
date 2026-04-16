using DamageTerror.Enums;

namespace DamageTerror.Helpers;

public static class JobDataTable
{
    public readonly record struct JobEntry(
        string Abbreviation,
        string FullName,
        JobRole Role,
        uint ClassJobId,
        Vector4 DefaultColor,
        bool IsBaseClass);

    private static readonly JobEntry[] AllEntries =
    [
        new("Pld", "Paladin", JobRole.Tank, 19, new(0.65882355f, 0.8235294f, 0.9019608f, 1.0f), false),
        new("War", "Warrior", JobRole.Tank, 21, new(0.8117647f, 0.14901961f, 0.12941177f, 1.0f), false),
        new("Drk", "Dark Knight", JobRole.Tank, 32, new(0.81960785f, 0.14901961f, 0.8f, 1.0f), false),
        new("Gnb", "Gunbreaker", JobRole.Tank, 37, new(0.47450981f, 0.42745098f, 0.18823530f, 1.0f), false),

        new("Whm", "White Mage", JobRole.Healer, 24, new(1.0f, 0.9411765f, 0.8627451f, 1.0f), false),
        new("Sch", "Scholar", JobRole.Healer, 28, new(0.5254902f, 0.34117648f, 1.0f, 1.0f), false),
        new("Ast", "Astrologian", JobRole.Healer, 33, new(1.0f, 0.90588236f, 0.2901961f, 1.0f), false),
        new("Sge", "Sage", JobRole.Healer, 40, new(0.5019608f, 0.627451f, 0.9411765f, 1.0f), false),

        new("Mnk", "Monk", JobRole.MeleeDps, 20, new(0.8392157f, 0.6117647f, 0.0f, 1.0f), false),
        new("Drg", "Dragoon", JobRole.MeleeDps, 22, new(0.25490198f, 0.39215687f, 0.8039216f, 1.0f), false),
        new("Nin", "Ninja", JobRole.MeleeDps, 30, new(0.6862745f, 0.09803922f, 0.39215687f, 1.0f), false),
        new("Sam", "Samurai", JobRole.MeleeDps, 34, new(0.89411765f, 0.42745098f, 0.015686275f, 1.0f), false),
        new("Rpr", "Reaper", JobRole.MeleeDps, 39, new(0.5882353f, 0.3529412f, 0.5647059f, 1.0f), false),
        new("Vpr", "Viper", JobRole.MeleeDps, 41, new(0.0627451f, 0.50980395f, 0.0627451f, 1.0f), false),

        new("Brd", "Bard", JobRole.RangedDps, 23, new(0.5686275f, 0.7294118f, 0.36862746f, 1.0f), false),
        new("Mch", "Machinist", JobRole.RangedDps, 31, new(0.43137255f, 0.88235295f, 0.8392157f, 1.0f), false),
        new("Dnc", "Dancer", JobRole.RangedDps, 38, new(0.8862745f, 0.6901961f, 0.6862745f, 1.0f), false),

        new("Blm", "Black Mage", JobRole.CasterDps, 25, new(0.64705884f, 0.4745098f, 0.8392157f, 1.0f), false),
        new("Smn", "Summoner", JobRole.CasterDps, 27, new(0.1764706f, 0.60784316f, 0.47058824f, 1.0f), false),
        new("Rdm", "Red Mage", JobRole.CasterDps, 35, new(0.9098039f, 0.48235294f, 0.48235294f, 1.0f), false),
        new("Pct", "Pictomancer", JobRole.CasterDps, 42, new(0.9882353f, 0.57254905f, 0.88235295f, 1.0f), false),
        new("Blu", "Blue Mage", JobRole.CasterDps, 36, new(0.30f, 0.55f, 0.90f, 1.0f), false),

        new("Gla", "Gladiator", JobRole.Tank, 1, new(0.65882355f, 0.8235294f, 0.9019608f, 1.0f), true),
        new("Mrd", "Marauder", JobRole.Tank, 3, new(0.8117647f, 0.14901961f, 0.12941177f, 1.0f), true),
        new("Cnj", "Conjurer", JobRole.Healer, 6, new(1.0f, 0.9411765f, 0.8627451f, 1.0f), true),
        new("Pgl", "Pugilist", JobRole.MeleeDps, 2, new(0.8392157f, 0.6117647f, 0.0f, 1.0f), true),
        new("Lnc", "Lancer", JobRole.MeleeDps, 4, new(0.25490198f, 0.39215687f, 0.8039216f, 1.0f), true),
        new("Arc", "Archer", JobRole.RangedDps, 5, new(0.5686275f, 0.7294118f, 0.36862746f, 1.0f), true),
        new("Rog", "Rogue", JobRole.MeleeDps, 29, new(0.6862745f, 0.09803922f, 0.39215687f, 1.0f), true),
        new("Thm", "Thaumaturge", JobRole.CasterDps, 7, new(0.64705884f, 0.4745098f, 0.8392157f, 1.0f), true),
        new("Acn", "Arcanist", JobRole.CasterDps, 26, new(0.1764706f, 0.60784316f, 0.47058824f, 1.0f), true),
    ];

    public static readonly string[] TankJobs = GetAbbreviations(JobRole.Tank, baseClasses: false);
    public static readonly string[] HealerJobs = GetAbbreviations(JobRole.Healer, baseClasses: false);
    public static readonly string[] MeleeDpsJobs = GetAbbreviations(JobRole.MeleeDps, baseClasses: false);
    public static readonly string[] RangedDpsJobs = GetAbbreviations(JobRole.RangedDps, baseClasses: false);
    public static readonly string[] CasterDpsJobs = GetAbbreviations(JobRole.CasterDps, baseClasses: false);
    public static readonly string[] BaseClassJobs = AllEntries.Where(e => e.IsBaseClass).Select(e => e.Abbreviation).ToArray();
    public static readonly string[] AllAbbreviations = AllEntries.Select(e => e.Abbreviation).ToArray();

    private static readonly Dictionary<string, JobEntry> Lookup = BuildLookup();

    private static string[] GetAbbreviations(JobRole role, bool baseClasses) =>
        AllEntries.Where(e => e.Role == role && e.IsBaseClass == baseClasses).Select(e => e.Abbreviation).ToArray();

    private static Dictionary<string, JobEntry> BuildLookup()
    {
        var dict = new Dictionary<string, JobEntry>(AllEntries.Length * 2, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in AllEntries)
        {
            dict[entry.Abbreviation] = entry;
            dict[entry.FullName.Replace(" ", "")] = entry;
        }
        var lmb = new JobEntry("Lmb", "Limit Break", JobRole.LimitBreak, 0, new(0.5f, 0.5f, 0.5f, 1.0f), false);
        dict["Lmb"] = lmb;
        dict["Limit Break"] = lmb;
        dict["LimitBreak"] = lmb;
        return dict;
    }

    public static bool TryGet(string key, out JobEntry entry) => Lookup.TryGetValue(key, out entry);

    public static JobRole GetRole(string job)
    {
        if (string.IsNullOrEmpty(job)) return JobRole.Default;
        return Lookup.TryGetValue(job, out var entry) ? entry.Role : JobRole.Default;
    }

    public static string GetFullName(string abbreviation)
    {
        if (string.IsNullOrEmpty(abbreviation)) return abbreviation;
        return Lookup.TryGetValue(abbreviation, out var entry) ? entry.FullName : abbreviation;
    }

    public static Vector4 GetDefaultColor(string job)
    {
        if (!string.IsNullOrEmpty(job) && Lookup.TryGetValue(job, out var entry))
            return entry.DefaultColor;
        return new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
    }

    public static uint? GetClassJobId(string job)
    {
        if (!string.IsNullOrEmpty(job) && Lookup.TryGetValue(job, out var entry) && entry.ClassJobId > 0)
            return entry.ClassJobId;
        return null;
    }
}
