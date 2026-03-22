using Newtonsoft.Json.Linq;

namespace DamageTerror.Helpers;

/// <summary>
/// Parses IINACT CombatData JSON events into strongly-typed models.
/// </summary>
public static class CombatDataParser
{
    /// <summary>
    /// Parse a CombatData JObject into an EncounterSnapshot.
    /// Returns null if the data is invalid.
    /// </summary>
    public static EncounterSnapshot? Parse(JObject data)
    {
        if (data["type"]?.ToString() != "CombatData")
            return null;

        var encounterObj = data["Encounter"] as JObject;
        var combatantObj = data["Combatant"] as JObject;
        if (encounterObj == null)
            return null;

        var snapshot = new EncounterSnapshot
        {
            Encounter = ParseEncounter(encounterObj, data["isActive"]?.ToString()),
            Combatants = ParseCombatants(combatantObj),
            Timestamp = DateTime.UtcNow,
        };

        return snapshot;
    }

    private static CombatEncounter ParseEncounter(JObject enc, string? isActive)
    {
        return new CombatEncounter
        {
            Title = GetString(enc, "title"),
            Duration = GetString(enc, "duration", "00:00"),
            ZoneName = GetString(enc, "CurrentZoneName"),
            EncDps = GetDouble(enc, "ENCDPS"),
            EncHps = GetDouble(enc, "ENCHPS"),
            TotalDamage = GetLong(enc, "damage"),
            TotalHealed = GetLong(enc, "healed"),
            Kills = GetInt(enc, "kills"),
            Deaths = GetInt(enc, "deaths"),
            IsActive = string.Equals(isActive, "true", StringComparison.OrdinalIgnoreCase),
        };
    }

    private static List<CombatantEntry> ParseCombatants(JObject? combatants)
    {
        var list = new List<CombatantEntry>();
        if (combatants == null)
            return list;

        foreach (var prop in combatants.Properties())
        {
            var c = prop.Value as JObject;
            if (c == null)
                continue;

            list.Add(new CombatantEntry
            {
                Name = prop.Name,
                Job = GetString(c, "Job"),
                EncDps = GetDouble(c, "ENCDPS"),
                EncHps = GetDouble(c, "ENCHPS"),
                Damage = GetLong(c, "damage"),
                Healed = GetLong(c, "healed"),
                DamagePercent = GetString(c, "damage%", "0%"),
                CritPct = GetDouble(c, "crithit%"),
                DirectHitPct = GetDouble(c, "DirectHitPct"),
                CritDirectHitPct = GetDouble(c, "CritDirectHitPct"),
                Deaths = GetInt(c, "deaths"),
                OverhealPct = GetDouble(c, "OverHealPct"),
                MaxHit = GetString(c, "maxhit"),
                MaxHitDamage = GetLong(c, "MAXHIT"),
                Last10Dps = GetDouble(c, "Last10DPS"),
                Last30Dps = GetDouble(c, "Last30DPS"),
                Last60Dps = GetDouble(c, "Last60DPS"),
            });
        }

        return list;
    }

    private static string GetString(JObject obj, string key, string defaultValue = "")
    {
        var token = obj[key];
        return token?.ToString() ?? defaultValue;
    }

    private static double GetDouble(JObject obj, string key)
    {
        var token = obj[key];
        if (token == null)
            return 0;

        var str = token.ToString();
        if (string.IsNullOrEmpty(str) || str == "---" || str == "∞")
            return 0;

        // Remove commas and percentage signs for parsing
        str = str.Replace(",", "").Replace("%", "").Trim();
        return double.TryParse(str, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var val) ? val : 0;
    }

    private static long GetLong(JObject obj, string key)
    {
        var token = obj[key];
        if (token == null)
            return 0;

        var str = token.ToString().Replace(",", "").Trim();
        if (string.IsNullOrEmpty(str) || str == "---")
            return 0;

        return long.TryParse(str, out var val) ? val : 0;
    }

    private static int GetInt(JObject obj, string key)
    {
        var token = obj[key];
        if (token == null)
            return 0;

        var str = token.ToString().Replace(",", "").Trim();
        if (string.IsNullOrEmpty(str) || str == "---")
            return 0;

        return int.TryParse(str, out var val) ? val : 0;
    }
}
