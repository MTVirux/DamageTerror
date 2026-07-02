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

    /// <summary>Sets the crit/DH percentages from raw hit counters. Crit-direct
    /// hits count toward both the crit and direct-hit percentages. Caller must
    /// guard against <paramref name="hits"/> being zero.</summary>
    public void SetHitRates(int crits, int directHits, int critDirectHits, int hits)
    {
        CritPct = (double)(crits + critDirectHits) / hits * 100.0;
        DirectHitPct = (double)(directHits + critDirectHits) / hits * 100.0;
        CritDirectHitPct = (double)critDirectHits / hits * 100.0;
    }
}
