namespace DamageTerror.Models;

public class CombatantEntry
{
    public string Name { get; set; } = string.Empty;

    public string Job { get; set; } = string.Empty;

    public double EncDps { get; set; }

    public double EncHps { get; set; }

    public double RaidDps { get; set; }

    public double RaidHps { get; set; }

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

    public string MaxHitSkillName => ExtractSkillName(MaxHit);

    public double PeakDps { get; set; }

    public string MaxHeal { get; set; } = string.Empty;

    public long MaxHealAmount { get; set; }

    public string MaxHealSkillName => ExtractSkillName(MaxHeal);

    private static string ExtractSkillName(string composite)
    {
        if (string.IsNullOrEmpty(composite)) return string.Empty;
        var idx = composite.LastIndexOf('-');
        return idx > 0 ? composite[..idx] : composite;
    }

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

    public int Stuns { get; set; }

    public List<SkillEntry> Skills { get; set; } = new();

    public List<SkillEntry> HealingSkills { get; set; } = new();

    public bool IsLocalPlayer { get; set; }

    /// <summary>Home world name (e.g. "Spriggan"). Resolved from party list at parse time, persisted with encounter history.</summary>
    public string HomeWorld { get; set; } = string.Empty;
}
