using Newtonsoft.Json.Linq;

namespace DamageTerror.Services;

internal static class DataSourceDispatcher
{
    public static void Dispatch(
        JObject data,
        Action<EncounterSnapshot>? onCombatData,
        Action<string, uint>? onPrimaryPlayerChanged,
        Action<string[]>? onLogLine)
    {
        var type = data["type"]?.ToString();

        switch (type)
        {
            case "CombatData":
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
                var lineArray = data["line"] as JArray;
                if (lineArray != null)
                {
                    var fields = lineArray.Select(t => t.ToString()).ToArray();
                    onLogLine?.Invoke(fields);
                }
                break;
        }
    }
}
