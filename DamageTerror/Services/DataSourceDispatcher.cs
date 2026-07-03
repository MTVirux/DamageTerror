namespace DamageTerror.Services;

internal static class DataSourceDispatcher
{
    public static void Dispatch(
        JObject data,
        Action<EncounterSnapshot>? onCombatData,
        Action<string, uint>? onPrimaryPlayerChanged,
        Action<string[]>? onLogLine,
        Action<JObject>? onRawCombatData = null)
    {
        var type = data["type"]?.ToString();

        switch (type)
        {
            case "CombatData":
                onRawCombatData?.Invoke(data);
                var snapshot = CombatDataParser.Parse(data);
                if (snapshot != null)
                    onCombatData?.Invoke(snapshot);
                break;

            case "ChangePrimaryPlayer":
                var charName = data["charName"]?.ToString() ?? string.Empty;
                var charId = data["charID"]?.ToObject<uint>() ?? 0;
                if (!string.IsNullOrEmpty(charName))
                    onPrimaryPlayerChanged?.Invoke(charName, charId);
                break;

            case "LogLine":
                if (data["line"] is JArray lineArray)
                {
                    var fields = lineArray.Select(t => t.ToString()).ToArray();
                    onLogLine?.Invoke(fields);
                }
                break;
        }
    }
}
