namespace DamageTerror.Helpers;

public static class EncounterSearchHelper
{
    public static bool MatchesFilter(EncounterSnapshot snapshot, string filter)
    {
        if (string.IsNullOrEmpty(filter)) return true;

        var enc = snapshot.Encounter;
        return enc.ZoneName.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || (enc.Title?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
            || (snapshot.PlayerName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
            || snapshot.Combatants.Any(c => c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || c.Job.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }
}
