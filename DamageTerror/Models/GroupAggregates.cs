namespace DamageTerror.Models;

public sealed class GroupAggregates
{
    // Totals (sum)
    public double Dps { get; init; }
    public double Hps { get; init; }
    public long Damage { get; init; }
    public long Healed { get; init; }
    public long DamageTaken { get; init; }
    public int Deaths { get; init; }
    public long Overheal { get; init; }
    public double InstantDps { get; init; }
    public double InstantHps { get; init; }
    public int SkillIssue { get; init; }
    public int DamageDown { get; init; }

    // Averages (mean)
    public double AvgDps { get; init; }
    public double AvgHps { get; init; }
    public double AvgCrit { get; init; }
    public double AvgDirectHit { get; init; }
    public double AvgCritDirectHit { get; init; }
    public double AvgOverhealPct { get; init; }
    public double AvgCritHealPct { get; init; }
    public double AvgHitRate { get; init; }

    // Max (best in group)
    public double PeakDps { get; init; }
    public long MaxHitValue { get; init; }
    public long MaxHealValue { get; init; }

    /// <summary>The encounter's own clock, which no combatant carries - it is passed in.</summary>
    public string Duration { get; init; } = string.Empty;

    public static GroupAggregates Compute(List<CombatantEntry> combatants, string duration = "")
    {
        if (combatants.Count == 0)
            return new GroupAggregates { Duration = duration };

        double sumDps = 0, sumHps = 0, sumInstantDps = 0, sumInstantHps = 0;
        long sumDamage = 0, sumHealed = 0, sumDamageTaken = 0, sumOverheal = 0;
        int sumDeaths = 0, sumSkillIssue = 0, sumDamageDown = 0;
        double sumCrit = 0, sumDH = 0, sumCDH = 0, sumOH = 0, sumCH = 0, sumHR = 0;
        double maxPeakDps = 0;
        long maxHit = 0, maxHeal = 0;

        foreach (var c in combatants)
        {
            sumDps += c.EncDps;
            sumHps += c.EncHps;
            sumDamage += c.Damage;
            sumHealed += c.Healed;
            sumDamageTaken += c.DamageTaken;
            sumDeaths += c.Deaths;
            sumSkillIssue += c.SkillIssue;
            sumDamageDown += c.DamageDown;
            sumOverheal += c.OverhealAmount;
            sumInstantDps += c.InstantDps;
            sumInstantHps += c.InstantHps;

            sumCrit += c.CritPct;
            sumDH += c.DirectHitPct;
            sumCDH += c.CritDirectHitPct;
            sumOH += c.OverhealPct;
            sumCH += c.CritHealPct;
            sumHR += c.HitRate;

            if (c.PeakDps > maxPeakDps) maxPeakDps = c.PeakDps;
            if (c.MaxHitDamage > maxHit) maxHit = c.MaxHitDamage;
            if (c.MaxHealAmount > maxHeal) maxHeal = c.MaxHealAmount;
        }

        var n = combatants.Count;
        return new GroupAggregates
        {
            Dps = sumDps,
            Hps = sumHps,
            Damage = sumDamage,
            Healed = sumHealed,
            DamageTaken = sumDamageTaken,
            Deaths = sumDeaths,
            SkillIssue = sumSkillIssue,
            DamageDown = sumDamageDown,
            Overheal = sumOverheal,
            InstantDps = sumInstantDps,
            InstantHps = sumInstantHps,
            AvgDps = sumDps / n,
            AvgHps = sumHps / n,
            AvgCrit = sumCrit / n,
            AvgDirectHit = sumDH / n,
            AvgCritDirectHit = sumCDH / n,
            AvgOverhealPct = sumOH / n,
            AvgCritHealPct = sumCH / n,
            AvgHitRate = sumHR / n,
            PeakDps = maxPeakDps,
            MaxHitValue = maxHit,
            MaxHealValue = maxHeal,
            Duration = duration,
        };
    }

    public static readonly GroupAggregates Empty = new();
}
