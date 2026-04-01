namespace DamageTerror.Helpers;

public static class JobNameHelper
{
    private static readonly Dictionary<string, string> JobFullNames = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Pld", "Paladin" },
        { "War", "Warrior" },
        { "Drk", "Dark Knight" },
        { "Gnb", "Gunbreaker" },

        { "Whm", "White Mage" },
        { "Sch", "Scholar" },
        { "Ast", "Astrologian" },
        { "Sge", "Sage" },

        { "Mnk", "Monk" },
        { "Drg", "Dragoon" },
        { "Nin", "Ninja" },
        { "Sam", "Samurai" },
        { "Rpr", "Reaper" },
        { "Vpr", "Viper" },

        { "Brd", "Bard" },
        { "Mch", "Machinist" },
        { "Dnc", "Dancer" },

        { "Blm", "Black Mage" },
        { "Smn", "Summoner" },
        { "Rdm", "Red Mage" },
        { "Pct", "Pictomancer" },
        { "Blu", "Blue Mage" },

        // Base classes
        { "Gla", "Gladiator" },
        { "Mrd", "Marauder" },
        { "Pgl", "Pugilist" },
        { "Lnc", "Lancer" },
        { "Arc", "Archer" },
        { "Cnj", "Conjurer" },
        { "Thm", "Thaumaturge" },
        { "Acn", "Arcanist" },
        { "Rog", "Rogue" },
    };

    public static string GetFullName(string abbreviation)
    {
        if (string.IsNullOrEmpty(abbreviation))
            return abbreviation;

        return JobFullNames.TryGetValue(abbreviation, out var fullName) ? fullName : abbreviation;
    }
}
