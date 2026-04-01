using Newtonsoft.Json;

namespace DamageTerror.Models;

public class EncounterSnapshot
{
    public CombatEncounter Encounter { get; set; } = new();

    public List<CombatantEntry> Combatants { get; set; } = new();

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
    public string PlayerName { get; set; } = string.Empty;
}
