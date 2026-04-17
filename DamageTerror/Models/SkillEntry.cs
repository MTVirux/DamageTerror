namespace DamageTerror.Models;

public sealed class SkillEntry
{
    public string Name { get; set; } = string.Empty;

    public long TotalDamage { get; set; }

    public int HitCount { get; set; }

    public double DamagePercent { get; set; }

    public double CritPct { get; set; }

    public double DirectHitPct { get; set; }

    public double CritDirectHitPct { get; set; }

    public SkillDamageType DamageType { get; set; }

    /// <summary>Optional nested entries for DoT/HoT tick breakdowns under this skill.</summary>
    public List<SkillEntry>? SubEntries { get; set; }
}
