namespace DamageTerror.Models;

/// <summary>
/// Parsed per-combatant data from the IINACT CombatData event.
/// </summary>
public class CombatantEntry
{
    public string Name { get; set; } = string.Empty;

    public string Job { get; set; } = string.Empty;

    public double EncDps { get; set; }

    public double EncHps { get; set; }

    public long Damage { get; set; }

    public long Healed { get; set; }

    public string DamagePercent { get; set; } = "0%";

    public double CritPct { get; set; }

    public double DirectHitPct { get; set; }

    public double CritDirectHitPct { get; set; }

    public int Deaths { get; set; }

    public double OverhealPct { get; set; }

    public string MaxHit { get; set; } = string.Empty;

    public long MaxHitDamage { get; set; }

    public double Last10Dps { get; set; }

    public double Last30Dps { get; set; }

    public double Last60Dps { get; set; }

    public List<SkillEntry> Skills { get; set; } = new();

    public List<SkillEntry> HealingSkills { get; set; } = new();

    public bool IsLocalPlayer { get; set; }
}
