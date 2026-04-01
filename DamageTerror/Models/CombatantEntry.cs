namespace DamageTerror.Models;

public class CombatantEntry
{
    public string Name { get; set; } = string.Empty;

    public string Job { get; set; } = string.Empty;

    public double EncDps { get; set; }

    public double EncHps { get; set; }

    public long Damage { get; set; }

    public long Healed { get; set; }

    public string DamagePercent { get; set; } = "0%";

    public string HealedPercent { get; set; } = "0%";

    public double CritPct { get; set; }

    public double DirectHitPct { get; set; }

    public double CritDirectHitPct { get; set; }

    public int Deaths { get; set; }

    public long DamageTaken { get; set; }

    public string DamageTakenPercent { get; set; } = "0%";

    public double OverhealPct { get; set; }

    public long OverhealAmount { get; set; }

    public string MaxHit { get; set; } = string.Empty;

    public long MaxHitDamage { get; set; }

    public double Last10Dps { get; set; }

    public double Last30Dps { get; set; }

    public double Last60Dps { get; set; }

    public double PeakDps { get; set; }

    public string MaxHeal { get; set; } = string.Empty;

    public long MaxHealAmount { get; set; }

    public int Swings { get; set; }

    public int Hits { get; set; }

    public int Misses { get; set; }

    public double HitRate { get; set; }

    public int CritHitCount { get; set; }

    public int DirectHitCount { get; set; }

    public int CritDirectHitCount { get; set; }

    public double BlockPct { get; set; }

    public double ParryPct { get; set; }

    public long HealsTaken { get; set; }

    public long AbsorbHeal { get; set; }

    public int Kills { get; set; }

    public double InstantDps { get; set; }

    public double InstantHps { get; set; }

    public double CritHealPct { get; set; }

    public int HealCount { get; set; }

    public string CombatantDuration { get; set; } = "00:00";

    public long DamageShield { get; set; }

    public string MaxHealWardName { get; set; } = string.Empty;

    public long MaxHealWardAmount { get; set; }

    public long PowerDrain { get; set; }

    public long PowerHeal { get; set; }

    public List<SkillEntry> Skills { get; set; } = new();

    public List<SkillEntry> HealingSkills { get; set; } = new();

    public bool IsLocalPlayer { get; set; }
}
