namespace DamageTerror.Models;

/// <summary>
/// Damage data for a single skill/ability used by a combatant.
/// </summary>
public class SkillEntry
{
    public string Name { get; set; } = string.Empty;

    public long TotalDamage { get; set; }

    public int HitCount { get; set; }

    public double DamagePercent { get; set; }

    public double CritPct { get; set; }

    public double DirectHitPct { get; set; }

    public double CritDirectHitPct { get; set; }
}
