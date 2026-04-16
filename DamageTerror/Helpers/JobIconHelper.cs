using Dalamud.Plugin.Services;
using DamageTerror.Enums;

namespace DamageTerror.Helpers;

public static class JobIconHelper
{
    private static readonly Dictionary<string, uint> ClassJobIdMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Pld", 19 }, { "paladin", 19 },
        { "War", 21 }, { "warrior", 21 },
        { "Drk", 32 }, { "darkknight", 32 },
        { "Gnb", 37 }, { "gunbreaker", 37 },

        { "Whm", 24 }, { "whitemage", 24 },
        { "Sch", 28 }, { "scholar", 28 },
        { "Ast", 33 }, { "astrologian", 33 },
        { "Sge", 40 }, { "sage", 40 },

        { "Mnk", 20 }, { "monk", 20 },
        { "Drg", 22 }, { "dragoon", 22 },
        { "Nin", 30 }, { "ninja", 30 },
        { "Sam", 34 }, { "samurai", 34 },
        { "Rpr", 39 }, { "reaper", 39 },
        { "Vpr", 41 }, { "viper", 41 },

        { "Brd", 23 }, { "bard", 23 },
        { "Mch", 31 }, { "machinist", 31 },
        { "Dnc", 38 }, { "dancer", 38 },

        { "Blm", 25 }, { "blackmage", 25 },
        { "Smn", 27 }, { "summoner", 27 },
        { "Rdm", 35 }, { "redmage", 35 },
        { "Pct", 42 }, { "pictomancer", 42 },
        { "Blu", 36 }, { "bluemage", 36 },

        { "Gla", 1 }, { "gladiator", 1 },
        { "Pgl", 2 }, { "pugilist", 2 },
        { "Mrd", 3 }, { "marauder", 3 },
        { "Lnc", 4 }, { "lancer", 4 },
        { "Arc", 5 }, { "archer", 5 },
        { "Cnj", 6 }, { "conjurer", 6 },
        { "Thm", 7 }, { "thaumaturge", 7 },
        { "Acn", 26 }, { "arcanist", 26 },
        { "Rog", 29 }, { "rogue", 29 },

        { "Crp", 8 }, { "Bsm", 9 }, { "Arm", 10 },
        { "Gsm", 11 }, { "Ltw", 12 }, { "Wvr", 13 },
        { "Alc", 14 }, { "Cul", 15 },
        { "Min", 16 }, { "Btn", 17 }, { "Fsh", 18 },
    };

    private static readonly Dictionary<string, uint> FixedIconMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Lmb", 103 }, { "Limit Break", 103 },
    };

    private static uint GetBaseOffset(JobIconStyle style) => style switch
    {
        JobIconStyle.Plain => 62000,
        _ => 62100,
    };

    public static uint? GetIconId(string job, JobIconStyle style = JobIconStyle.Framed,
        Dictionary<string, uint>? customIcons = null)
    {
        if (string.IsNullOrEmpty(job))
            return null;

        if (style == JobIconStyle.Custom
            && customIcons != null
            && customIcons.TryGetValue(job, out var customId)
            && customId != 0)
            return customId;

        if (FixedIconMap.TryGetValue(job, out var fixedId))
            return fixedId;

        if (!ClassJobIdMap.TryGetValue(job, out var classJobId))
            return null;

        var offset = style == JobIconStyle.Custom ? GetBaseOffset(JobIconStyle.Framed) : GetBaseOffset(style);
        return offset + classJobId;
    }

    /// <summary>All distinct job abbreviations (short 3-letter form).</summary>
    public static IEnumerable<string> AllJobAbbreviations =>
        ClassJobIdMap.Keys.Where(k => k.Length <= 3);
}
