using Dalamud.Plugin.Services;

namespace DamageTerror.Helpers;

/// <summary>
/// Maps ACT job abbreviation strings to FFXIV game icon IDs for job icons.
/// </summary>
public static class JobIconHelper
{
    // Map ACT job abbreviation → FFXIV job icon ID (from game data, 062100+ range)
    // These are the standard high-res job icons used in the game UI.
    private static readonly Dictionary<string, uint> JobIconMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Tanks
        { "Pld", 062119 }, { "paladin", 062119 },
        { "War", 062121 }, { "warrior", 062121 },
        { "Drk", 062132 }, { "darkknight", 062132 },
        { "Gnb", 062137 }, { "gunbreaker", 062137 },

        // Healers
        { "Whm", 062124 }, { "whitemage", 062124 },
        { "Sch", 062128 }, { "scholar", 062128 },
        { "Ast", 062133 }, { "astrologian", 062133 },
        { "Sge", 062140 }, { "sage", 062140 },

        // Melee DPS
        { "Mnk", 062120 }, { "monk", 062120 },
        { "Drg", 062122 }, { "dragoon", 062122 },
        { "Nin", 062130 }, { "ninja", 062130 },
        { "Sam", 062134 }, { "samurai", 062134 },
        { "Rpr", 062139 }, { "reaper", 062139 },
        { "Vpr", 062141 }, { "viper", 062141 },

        // Ranged Physical DPS
        { "Brd", 062123 }, { "bard", 062123 },
        { "Mch", 062131 }, { "machinist", 062131 },
        { "Dnc", 062138 }, { "dancer", 062138 },

        // Caster DPS
        { "Blm", 062125 }, { "blackmage", 062125 },
        { "Smn", 062127 }, { "summoner", 062127 },
        { "Rdm", 062135 }, { "redmage", 062135 },
        { "Pct", 062142 }, { "pictomancer", 062142 },
        { "Blu", 062136 }, { "bluemage", 062136 },

        // Crafters/Gatherers (unlikely in combat but handle gracefully)
        { "Crp", 062108 }, { "Bsm", 062109 }, { "Arm", 062110 },
        { "Gsm", 062111 }, { "Ltw", 062112 }, { "Wvr", 062113 },
        { "Alc", 062114 }, { "Cul", 062115 },
        { "Min", 062116 }, { "Btn", 062117 }, { "Fsh", 062118 },
    };

    /// <summary>
    /// Returns the game icon ID for a given job abbreviation, or null if unknown.
    /// </summary>
    public static uint? GetIconId(string job)
    {
        if (string.IsNullOrEmpty(job))
            return null;

        return JobIconMap.TryGetValue(job, out var iconId) ? iconId : null;
    }
}
