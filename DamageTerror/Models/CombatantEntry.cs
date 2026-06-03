namespace DamageTerror.Models;

public sealed class CombatantEntry
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
        var name = idx > 0 ? composite[..idx] : composite;
        return SkillNameOverrides.Apply(name);
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

    public int SkillIssue { get; set; }

    public int DamageDown { get; set; }

    public int Positionals { get; set; }

    public int PositionalHits { get; set; }

    public int PositionalMisses { get; set; }

    public double PositionalPct => Positionals > 0 ? (double)PositionalHits / Positionals * 100.0 : 0.0;

    public List<SkillEntry> Skills { get; set; } = new();

    public List<SkillEntry> HealingSkills { get; set; } = new();

    public bool IsLocalPlayer { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public int DpsRank { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public int DpsRankTotal { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public int HpsRank { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public int HpsRankTotal { get; set; }

    /// <summary>Home world name (e.g. "Spriggan"). Resolved from party list at parse time, persisted with encounter history.</summary>
    public string HomeWorld { get; set; } = string.Empty;

    /// <summary>
    /// Resets all combat-state fields (live numbers) on this combatant to
    /// zero / default for replay. Preserves metadata (name, job, recorded
    /// skills, recorded statuses) — only zeros numeric live-stats fields.
    /// </summary>
    public void ResetCombatStateForReplay()
    {
        Damage = 0;
        Healed = 0;
        DamageTaken = 0;
        EncDps = 0;
        EncHps = 0;
        RaidDps = 0;
        RaidHps = 0;
        InstantDps = 0;
        InstantHps = 0;
        PeakDps = 0;
        DamagePercent = "0%";
        HealedPercent = "0%";
        DamageTakenPercent = "0%";
        MaxHit = string.Empty;
        MaxHitDamage = 0;
        MaxHeal = string.Empty;
        MaxHealAmount = 0;
        Hits = 0;
        Misses = 0;
        Swings = 0;
        HitRate = 0;
        CritHitCount = 0;
        DirectHitCount = 0;
        CritDirectHitCount = 0;
        CritPct = 0;
        DirectHitPct = 0;
        CritDirectHitPct = 0;
        Deaths = 0;
        Kills = 0;
        OverhealAmount = 0;
        OverhealPct = 0;
        HealsTaken = 0;
        AbsorbHeal = 0;
        HealCount = 0;
        CritHealPct = 0;
        PowerDrain = 0;
        PowerHeal = 0;
        Stuns = 0;
        SkillIssue = 0;
        DamageDown = 0;
        Positionals = 0;
        PositionalHits = 0;
        PositionalMisses = 0;
        DamageShield = 0;
        MaxHealWardName = string.Empty;
        MaxHealWardAmount = 0;
        Skills = new List<SkillEntry>();
        HealingSkills = new List<SkillEntry>();
    }
}
