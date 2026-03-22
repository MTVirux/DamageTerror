namespace DamageTerror.Helpers;

/// <summary>
/// Maps ACT job abbreviations to full job names.
/// </summary>
public static class JobNameHelper
{
    private static readonly Dictionary<string, string> JobFullNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // Tanks
        { "Pld", "Paladin" },
        { "War", "Warrior" },
        { "Drk", "Dark Knight" },
        { "Gnb", "Gunbreaker" },

        // Healers
        { "Whm", "White Mage" },
        { "Sch", "Scholar" },
        { "Ast", "Astrologian" },
        { "Sge", "Sage" },

        // Melee DPS
        { "Mnk", "Monk" },
        { "Drg", "Dragoon" },
        { "Nin", "Ninja" },
        { "Sam", "Samurai" },
        { "Rpr", "Reaper" },
        { "Vpr", "Viper" },

        // Ranged Physical DPS
        { "Brd", "Bard" },
        { "Mch", "Machinist" },
        { "Dnc", "Dancer" },

        // Caster DPS
        { "Blm", "Black Mage" },
        { "Smn", "Summoner" },
        { "Rdm", "Red Mage" },
        { "Pct", "Pictomancer" },
        { "Blu", "Blue Mage" },
    };

    /// <summary>
    /// Returns the full job name for a given abbreviation, or the abbreviation itself if unknown.
    /// </summary>
    public static string GetFullName(string abbreviation)
    {
        if (string.IsNullOrEmpty(abbreviation))
            return abbreviation;

        return JobFullNames.TryGetValue(abbreviation, out var fullName) ? fullName : abbreviation;
    }
}
