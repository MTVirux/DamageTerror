namespace DamageTerror.Models;

/// <summary>
/// A snapshot of a complete encounter at a point in time.
/// Wraps the encounter summary and the list of combatants.
/// </summary>
public class EncounterSnapshot
{
    public CombatEncounter Encounter { get; set; } = new();

    public List<CombatantEntry> Combatants { get; set; } = new();

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
