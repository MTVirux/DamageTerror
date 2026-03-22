namespace DamageTerror.Models;

/// <summary>
/// Parsed encounter-level data from the IINACT CombatData event.
/// </summary>
public class CombatEncounter
{
    public string Title { get; set; } = string.Empty;

    public string Duration { get; set; } = "00:00";

    public string ZoneName { get; set; } = string.Empty;

    public double EncDps { get; set; }

    public double EncHps { get; set; }

    public long TotalDamage { get; set; }

    public long TotalHealed { get; set; }

    public int Kills { get; set; }

    public int Deaths { get; set; }

    public bool IsActive { get; set; }
}
