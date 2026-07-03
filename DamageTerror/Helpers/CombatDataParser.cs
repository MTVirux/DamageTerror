namespace DamageTerror.Helpers;

public static class CombatDataParser
{
    public static EncounterSnapshot? Parse(JObject data)
    {
        if (data["type"]?.ToString() != "CombatData")
            return null;

        if (data["Encounter"] is not JObject encounterObj)
            return null;

        var combatantObj = data["Combatant"] as JObject;

        var snapshot = new EncounterSnapshot
        {
            Encounter = ParseEncounter(encounterObj, data["isActive"]?.ToString()),
            Combatants = ParseCombatants(combatantObj),
            Timestamp = DateTime.UtcNow,
        };

        var raidDps = snapshot.Encounter.EncDps;
        var raidHps = snapshot.Encounter.EncHps;
        foreach (var c in snapshot.Combatants)
        {
            c.RaidDps = raidDps;
            c.RaidHps = raidHps;
        }

        ResolveEncounterTitle(snapshot);

        return snapshot;
    }

    // IINACT names encounters after the first player it sees take damage,
    // which in alliance / field-operation content is usually a random teammate.
    private static void ResolveEncounterTitle(EncounterSnapshot snapshot)
    {
        var title = snapshot.Encounter.Title;

        if (!string.IsNullOrEmpty(title))
        {
            var match = snapshot.Combatants.Find(c =>
                string.Equals(c.Name, title, StringComparison.OrdinalIgnoreCase));
            if (match != null && string.IsNullOrEmpty(match.Job))
                return;
        }

        CombatantEntry? boss = null;
        long maxDamageTaken = 0;
        foreach (var c in snapshot.Combatants)
        {
            if (!string.IsNullOrEmpty(c.Job)) continue;
            if (c.DamageTaken > maxDamageTaken)
            {
                boss = c;
                maxDamageTaken = c.DamageTaken;
            }
        }

        if (boss != null)
            snapshot.Encounter.Title = boss.Name;
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
            if (prop.Value is not JObject c)
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
                HealedPercent = GetString(c, "healed%", "0%"),
                CritPct = GetDouble(c, "crithit%"),
                DirectHitPct = GetDouble(c, "DirectHitPct"),
                CritDirectHitPct = GetDouble(c, "CritDirectHitPct"),
                Deaths = GetInt(c, "deaths"),
                DamageTaken = GetLong(c, "damagetaken"),
                DamageTakenPercent = GetString(c, "damagetaken%", "0%"),
                OverhealPct = GetDouble(c, "OverHealPct"),
                OverhealAmount = GetLong(c, "overHeal"),
                MaxHit = GetString(c, "maxhit"),
                MaxHitDamage = GetLong(c, "MAXHIT"),
                MaxHeal = GetString(c, "maxheal"),
                MaxHealAmount = GetLong(c, "MAXHEAL"),
                Swings = GetInt(c, "swings"),
                Hits = GetInt(c, "hits"),
                Misses = GetInt(c, "misses"),
                HitRate = GetDouble(c, "tohit"),
                CritHitCount = GetInt(c, "crithits"),
                DirectHitCount = GetInt(c, "DirectHitCount"),
                CritDirectHitCount = GetInt(c, "CritDirectHitCount"),
                BlockPct = GetDouble(c, "BlockPct"),
                ParryPct = GetDouble(c, "ParryPct"),
                HealsTaken = GetLong(c, "healstaken"),
                AbsorbHeal = GetLong(c, "absorbHeal"),
                Kills = GetInt(c, "kills"),
                InstantDps = GetDouble(c, "DPS"),
                InstantHps = GetDouble(c, "HPS"),
                CritHealPct = GetDouble(c, "critheal%"),
                HealCount = GetInt(c, "cures"),
                CombatantDuration = GetString(c, "DURATION", "00:00"),
                DamageShield = GetLong(c, "damageShield"),
                MaxHealWardName = GetString(c, "maxhealward"),
                MaxHealWardAmount = GetLong(c, "MAXHEALWARD"),
                PowerDrain = GetLong(c, "powerdrain"),
                PowerHeal = GetLong(c, "powerheal"),
            });
        }

        return list;
    }

    private const string NullPlaceholder = "---";
    private const string InfinityPlaceholder = "∞";

    private static string GetString(JObject obj, string key, string defaultValue = "")
        => obj[key]?.ToString() ?? defaultValue;

    private static string? SanitizeNumericToken(JObject obj, string key)
    {
        var token = obj[key];
        if (token == null)
            return null;

        var str = token.ToString();
        if (string.IsNullOrEmpty(str) || str == NullPlaceholder || str == InfinityPlaceholder)
            return null;

        return str.Replace(",", "").Replace("%", "").Trim();
    }

    private static double GetDouble(JObject obj, string key)
        => SanitizeNumericToken(obj, key) is { } str
            && double.TryParse(str, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var val)
            ? val : 0;

    private static long GetLong(JObject obj, string key)
        => SanitizeNumericToken(obj, key) is { } str && long.TryParse(str, out var val) ? val : 0;

    private static int GetInt(JObject obj, string key)
        => SanitizeNumericToken(obj, key) is { } str && int.TryParse(str, out var val) ? val : 0;
}
