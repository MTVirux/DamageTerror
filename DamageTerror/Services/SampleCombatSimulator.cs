namespace DamageTerror.Services;


/// <summary>
/// Mutates a sample EncounterSnapshot in-place each tick to simulate active combat.
/// Every stat is accumulated from the generated event stream, so cumulative numbers
/// agree with each other: skill totals sum to the combatant's damage, hits plus
/// misses equal swings, and the encounter totals are the sum of the combatants.
/// </summary>
internal sealed class SampleCombatSimulator
{
    private static Random Rng => Random.Shared;

    private const float GraphInterval = 0.5f;
    private const float SkillInterval = 0.8f;
    private const float StatusInterval = 2.0f;
    private const float SpawnInterval = 0.5f;
    private const float ItemInterval = 40f;

    private readonly EncounterSnapshot snapshot;
    private readonly DateTime startTime;
    private readonly Func<CombatantEntry?>? combatantFactory;
    private readonly List<SimState> states = new();
    private readonly Dictionary<string, List<ActiveBuff>> activeBuffs = new(StringComparer.OrdinalIgnoreCase);

    private float lastGraphSampleTime;
    private float lastSkillEventTime;
    private float lastStatusTickTime;
    private float lastSpawnTime;

    public bool IsRunning { get; private set; }

    public SampleCombatSimulator(EncounterSnapshot snapshot, Func<CombatantEntry?>? combatantFactory = null)
    {
        this.snapshot = snapshot;
        this.startTime = DateTime.UtcNow;
        this.combatantFactory = combatantFactory;

        var enc = snapshot.Encounter;
        enc.IsActive = true;
        enc.Duration = "00:00";
        enc.TotalDamage = 0;
        enc.TotalHealed = 0;
        enc.EncDps = 0;
        enc.EncHps = 0;
        enc.Kills = 0;
        enc.Deaths = 0;

        snapshot.GraphData.Clear();
        snapshot.SkillEvents.Clear();
        snapshot.DamageTakenEvents.Clear();
        snapshot.ItemEvents.Clear();
        snapshot.StatusHistory.Clear();
        snapshot.StatusesReceived.Clear();

        foreach (var c in snapshot.Combatants)
            states.Add(CreateState(c, 0f));

        IsRunning = true;
    }

    /// <summary>
    /// Called each frame to update the snapshot with simulated combat progression.
    /// </summary>
    public void Tick()
    {
        if (!IsRunning) return;

        var elapsed = (float)(DateTime.UtcNow - startTime).TotalSeconds;
        if (elapsed < 0.01f) return;

        snapshot.Encounter.Duration = SimulatorHelpers.FormatDuration(elapsed);

        TickSpawns(elapsed);
        QueueProgress(elapsed);

        if (elapsed - lastSkillEventTime >= SkillInterval)
        {
            lastSkillEventTime = elapsed;
            TickSkillEvents(elapsed);
            UpdateShares();
        }

        if (elapsed - lastStatusTickTime >= StatusInterval)
        {
            lastStatusTickTime = elapsed;
            TickStatuses(elapsed);
        }

        UpdateRates(elapsed);

        if (elapsed - lastGraphSampleTime >= GraphInterval)
        {
            var interval = elapsed - lastGraphSampleTime;
            lastGraphSampleTime = elapsed;
            SampleGraphs(elapsed, interval);
        }
    }

    private static SimState CreateState(CombatantEntry c, float joinedAt)
    {
        var state = new SimState(c, joinedAt);
        c.ResetStats();
        ResetSkills(c.Skills);
        ResetSkills(c.HealingSkills);
        return state;
    }

    private static void ResetSkills(List<SkillEntry> skills)
    {
        foreach (var s in skills)
        {
            ResetSkill(s);
            if (s.SubEntries == null) continue;
            foreach (var sub in s.SubEntries)
                ResetSkill(sub);
        }
    }

    private static void ResetSkill(SkillEntry skill)
    {
        skill.TotalDamage = 0;
        skill.HitCount = 0;
        skill.DamagePercent = 0;
        skill.CritPct = 0;
        skill.DirectHitPct = 0;
        skill.CritDirectHitPct = 0;
    }

    private void TickSpawns(float elapsed)
    {
        if (combatantFactory == null || elapsed - lastSpawnTime < SpawnInterval) return;
        lastSpawnTime = elapsed;

        var newCombatant = combatantFactory();
        if (newCombatant == null) return;

        snapshot.Combatants.Add(newCombatant);
        states.Add(CreateState(newCombatant, elapsed));
    }

    /// <summary>Advances each combatant's damage / healing target along its curve and queues
    /// the difference, which the next batch of skill events pays out.</summary>
    private void QueueProgress(float elapsed)
    {
        for (var i = 0; i < states.Count; i++)
        {
            var s = states[i];
            var t = elapsed - s.JoinedAt;
            if (t <= 0f) continue;

            var damageTarget = s.BaseDps * t * (1.0 + 0.15 * Math.Sin(t * (0.3 + i * 0.07)));
            if (damageTarget > s.QueuedDamage)
            {
                s.PendingDamage += (long)(damageTarget - s.QueuedDamage);
                s.QueuedDamage = damageTarget;
            }

            var healTarget = s.BaseHps * t * (1.0 + 0.2 * Math.Sin(t * (0.25 + i * 0.05)));
            if (healTarget > s.QueuedHeal)
            {
                s.PendingHeal += (long)(healTarget - s.QueuedHeal);
                s.QueuedHeal = healTarget;
            }
        }
    }

    private void TickSkillEvents(float elapsed)
    {
        foreach (var s in states)
        {
            var c = s.Combatant;

            if (s.PendingDamage > 0 && c.Skills.Count > 0 && Rng.NextDouble() < 0.75)
            {
                var amount = (long)(s.PendingDamage * (0.6 + Rng.NextDouble() * 0.4));
                if (amount > 0)
                {
                    s.PendingDamage -= amount;
                    EmitDamage(s, elapsed, amount);
                }
            }

            if (s.PendingHeal > 0 && c.HealingSkills.Count > 0 && Rng.NextDouble() < 0.8)
            {
                var amount = (long)(s.PendingHeal * (0.6 + Rng.NextDouble() * 0.4));
                if (amount > 0)
                {
                    s.PendingHeal -= amount;
                    EmitHeal(s, elapsed, amount);
                }
            }

            if (Rng.NextDouble() < 0.15)
                EmitDamageTaken(s, elapsed);

            if (elapsed - s.LastItemUse >= ItemInterval && Rng.NextDouble() < 0.2)
            {
                s.LastItemUse = elapsed;
                snapshot.ItemEvents.GetOrAdd(c.Name).Add(new SkillUseEvent
                {
                    TimeSec = elapsed,
                    SkillName = SampleJobData.Items[Rng.Next(SampleJobData.Items.Length)],
                });
            }
        }
    }

    private void EmitDamage(SimState s, float elapsed, long amount)
    {
        var c = s.Combatant;
        var skill = c.Skills[Rng.Next(c.Skills.Count)];
        var isTick = skill.SubEntries is { Count: > 0 } && Rng.NextDouble() < 0.35;
        var isCrit = Rng.NextDouble() < s.CritChance;
        var isDirectHit = Rng.NextDouble() < s.DirectHitChance;

        c.Damage += amount;
        c.Swings++;
        c.Hits++;
        if (Rng.NextDouble() < 0.04)
        {
            c.Swings++;
            c.Misses++;
        }
        c.HitRate = SimulatorHelpers.Percent(c.Hits, c.Swings);

        if (isCrit) c.CritHitCount++;
        if (isDirectHit) c.DirectHitCount++;
        if (isCrit && isDirectHit) c.CritDirectHitCount++;
        c.CritPct = SimulatorHelpers.Percent(c.CritHitCount, c.Hits);
        c.DirectHitPct = SimulatorHelpers.Percent(c.DirectHitCount, c.Hits);
        c.CritDirectHitPct = SimulatorHelpers.Percent(c.CritDirectHitCount, c.Hits);

        if (amount > c.MaxHitDamage)
        {
            c.MaxHitDamage = amount;
            c.MaxHit = SimulatorHelpers.FormatMaxLabel(skill.Name, amount);
        }

        if (s.IsMelee && !isTick && Rng.NextDouble() < 0.35)
        {
            if (Rng.NextDouble() < 0.85) c.PositionalHits++;
            else c.PositionalMisses++;
            c.Positionals = c.PositionalHits + c.PositionalMisses;
        }

        if (s.SpendsMana && Rng.NextDouble() < 0.4)
            c.PowerDrain += Rng.Next(200, 800);

        if (Rng.NextDouble() < 0.02)
            c.Kills++;

        AccumulateSkill(s, isTick ? skill.SubEntries![0] : skill, amount, isCrit, isDirectHit);
        SimulatorHelpers.RecomputeSkillPercents(c.Skills);

        snapshot.SkillEvents.GetOrAdd(c.Name).Add(new SkillUseEvent
        {
            TimeSec = elapsed,
            SkillName = skill.Name,
            Amount = amount,
            IsCrit = isCrit,
            IsDirectHit = isDirectHit,
            IsDoTTick = isTick,
        });
    }

    private void EmitHeal(SimState s, float elapsed, long amount)
    {
        var c = s.Combatant;
        var skill = c.HealingSkills[Rng.Next(c.HealingSkills.Count)];
        var isTick = skill.SubEntries is { Count: > 0 } && Rng.NextDouble() < 0.35;
        var isCrit = Rng.NextDouble() < s.CritChance;

        c.Healed += amount;
        c.HealCount++;
        if (isCrit) s.CritHeals++;
        c.CritHealPct = SimulatorHelpers.Percent(s.CritHeals, c.HealCount);

        c.OverhealAmount += (long)(amount * Rng.NextDouble() * 0.5);
        c.OverhealPct = SimulatorHelpers.OverhealPct(c.Healed, c.OverhealAmount);

        if (amount > c.MaxHealAmount)
        {
            c.MaxHealAmount = amount;
            c.MaxHeal = SimulatorHelpers.FormatMaxLabel(skill.Name, amount);
        }

        if (s.ShieldsAllies && Rng.NextDouble() < 0.3)
        {
            var ward = (long)(amount * (0.4 + Rng.NextDouble() * 0.6));
            c.DamageShield += ward;
            c.AbsorbHeal += (long)(ward * (0.5 + Rng.NextDouble() * 0.4));
            if (ward > c.MaxHealWardAmount)
            {
                c.MaxHealWardAmount = ward;
                c.MaxHealWardName = skill.Name;
            }
        }

        if (s.SpendsMana && Rng.NextDouble() < 0.3)
            c.PowerHeal += Rng.Next(300, 1200);

        states[Rng.Next(states.Count)].Combatant.HealsTaken += amount;

        AccumulateSkill(s, isTick ? skill.SubEntries![0] : skill, amount, isCrit, isDirectHit: false);
        SimulatorHelpers.RecomputeSkillPercents(c.HealingSkills);

        snapshot.SkillEvents.GetOrAdd(c.Name).Add(new SkillUseEvent
        {
            TimeSec = elapsed,
            SkillName = skill.Name,
            Amount = amount,
            IsHeal = true,
            IsCrit = isCrit,
            IsHoTTick = isTick,
        });
    }

    private void EmitDamageTaken(SimState s, float elapsed)
    {
        var c = s.Combatant;
        var amount = (long)(Rng.Next(10000, 60000) * (s.IsTank ? 1.8 : 1.0));

        c.DamageTaken += amount;

        s.HitsTaken++;
        if (s.IsTank)
        {
            if (Rng.NextDouble() < 0.2) s.Blocks++;
            if (Rng.NextDouble() < 0.25) s.Parries++;
            c.BlockPct = SimulatorHelpers.Percent(s.Blocks, s.HitsTaken);
            c.ParryPct = SimulatorHelpers.Percent(s.Parries, s.HitsTaken);
        }

        if (Rng.NextDouble() < 0.03) c.Deaths++;
        if (Rng.NextDouble() < 0.05) c.Stuns++;
        if (Rng.NextDouble() < 0.06) c.SkillIssue++;
        if (Rng.NextDouble() < 0.05) c.DamageDown++;

        snapshot.DamageTakenEvents.GetOrAdd(c.Name).Add(new SkillUseEvent
        {
            TimeSec = elapsed,
            SkillName = SampleJobData.BossSkills[Rng.Next(SampleJobData.BossSkills.Length)],
            Amount = amount,
        });
    }

    private static void AccumulateSkill(SimState s, SkillEntry entry, long amount, bool isCrit, bool isDirectHit)
    {
        entry.TotalDamage += amount;
        entry.HitCount++;

        var counters = s.SkillCounters.GetValueOrDefault(entry.Name);
        if (isCrit && isDirectHit) counters.CritDirectHits++;
        else if (isCrit) counters.Crits++;
        else if (isDirectHit) counters.DirectHits++;
        s.SkillCounters[entry.Name] = counters;

        entry.SetHitRates(counters.Crits, counters.DirectHits, counters.CritDirectHits, entry.HitCount);
    }

    private void UpdateRates(float elapsed)
    {
        long totalDamage = 0, totalHealed = 0;
        var totalDeaths = 0;
        var totalKills = 0;

        for (var i = 0; i < states.Count; i++)
        {
            var s = states[i];
            var c = s.Combatant;
            var t = Math.Max(0.01f, elapsed - s.JoinedAt);

            c.EncDps = c.Damage / (double)t;
            c.EncHps = c.Healed / (double)t;
            c.InstantDps = s.BaseDps * (1.0 + 0.15 * Math.Sin(t * (0.3 + i * 0.07))) * (0.9 + Rng.NextDouble() * 0.2);
            c.InstantHps = s.BaseHps * (1.0 + 0.2 * Math.Sin(t * (0.25 + i * 0.05))) * (0.85 + Rng.NextDouble() * 0.3);
            c.PeakDps = Math.Max(c.PeakDps, c.InstantDps);
            c.CombatantDuration = snapshot.Encounter.Duration;
            c.EncounterName = snapshot.Encounter.Title;

            totalDamage += c.Damage;
            totalHealed += c.Healed;
            totalDeaths += c.Deaths;
            totalKills += c.Kills;
        }

        var enc = snapshot.Encounter;
        enc.TotalDamage = totalDamage;
        enc.TotalHealed = totalHealed;
        enc.EncDps = totalDamage / (double)elapsed;
        enc.EncHps = totalHealed / (double)elapsed;
        enc.Deaths = totalDeaths;
        enc.Kills = totalKills;

        foreach (var s in states)
        {
            s.Combatant.RaidDps = enc.EncDps;
            s.Combatant.RaidHps = enc.EncHps;
        }
    }

    /// <summary>Refreshes the formatted share strings. Kept off the per-frame path
    /// because the underlying totals only move when skill events fire.</summary>
    private void UpdateShares()
    {
        long totalDamage = 0, totalHealed = 0, totalDamageTaken = 0;
        foreach (var s in states)
        {
            totalDamage += s.Combatant.Damage;
            totalHealed += s.Combatant.Healed;
            totalDamageTaken += s.Combatant.DamageTaken;
        }

        foreach (var s in states)
        {
            var c = s.Combatant;
            c.DamagePercent = SimulatorHelpers.FormatPercent(c.Damage, totalDamage);
            c.HealedPercent = SimulatorHelpers.FormatPercent(c.Healed, totalHealed);
            c.DamageTakenPercent = SimulatorHelpers.FormatPercent(c.DamageTaken, totalDamageTaken);
        }
    }

    private void SampleGraphs(float elapsed, float interval)
    {
        foreach (var s in states)
        {
            var c = s.Combatant;
            var dtps = (float)((c.DamageTaken - s.GraphDamageTaken) / interval);
            s.GraphDamageTaken = c.DamageTaken;

            snapshot.GraphData.GetOrAdd(c.Name).Add(new GraphSample
            {
                TimeSec = elapsed,
                Dps = (float)c.InstantDps,
                Hps = (float)c.InstantHps,
                Dtps = dtps,
            });
        }
    }

    private void TickStatuses(float elapsed)
    {
        foreach (var s in states)
        {
            var c = s.Combatant;
            var buffs = activeBuffs.GetOrAdd(c.Name);

            for (var j = buffs.Count - 1; j >= 0; j--)
            {
                var b = buffs[j];
                if (elapsed >= b.ExpiresAt)
                {
                    b.Application.RemovedAtSec = elapsed;
                    buffs.RemoveAt(j);
                }
            }

            var jobBuffs = SampleDataGenerator.GetJobBuffs(c.Job);
            var jobDebuffs = SampleDataGenerator.GetJobDebuffs(c.Job);

            foreach (var (statusId, statusName, duration, isHot) in jobBuffs)
            {
                if (buffs.Exists(b => b.Application.StatusId == statusId)) continue;
                if (Rng.NextDouble() > 0.25) continue;

                var app = new StatusApplication
                {
                    StatusId = statusId,
                    StatusName = statusName,
                    SourceName = c.Name,
                    TargetName = c.Name, // self-buff default
                    AppliedAtSec = elapsed,
                    Duration = duration,
                    IsBuff = true,
                    IsHoT = isHot,
                };

                if (isHot && Rng.NextDouble() < 0.5)
                    app.TargetName = states[Rng.Next(states.Count)].Combatant.Name;

                buffs.Add(new ActiveBuff { Application = app, ExpiresAt = elapsed + duration });
                AddStatusToSnapshot(app);
            }

            foreach (var (statusId, statusName, duration, isDot) in jobDebuffs)
            {
                if (buffs.Exists(b => b.Application.StatusId == statusId)) continue;
                if (Rng.NextDouble() > 0.3) continue;

                var app = new StatusApplication
                {
                    StatusId = statusId,
                    StatusName = statusName,
                    SourceName = c.Name,
                    TargetName = c.Name,
                    AppliedAtSec = elapsed,
                    Duration = duration,
                    IsBuff = false,
                    IsDoT = isDot,
                };

                buffs.Add(new ActiveBuff { Application = app, ExpiresAt = elapsed + duration });
                AddStatusToSnapshot(app);
            }
        }
    }

    private void AddStatusToSnapshot(StatusApplication app)
    {
        snapshot.StatusHistory.GetOrAdd(app.SourceName).Add(app);
        snapshot.StatusesReceived.GetOrAdd(app.TargetName).Add(app);
    }

    public void Stop()
    {
        IsRunning = false;
        snapshot.Encounter.IsActive = false;
    }

    private sealed class SimState
    {
        public SimState(CombatantEntry combatant, float joinedAt)
        {
            Combatant = combatant;
            BaseDps = combatant.EncDps;
            BaseHps = combatant.EncHps;
            CritChance = combatant.CritPct > 0 ? combatant.CritPct / 100.0 : 0.2;
            DirectHitChance = combatant.DirectHitPct > 0 ? combatant.DirectHitPct / 100.0 : 0.25;
            JoinedAt = joinedAt;
            LastItemUse = joinedAt;

            var role = JobRegistry.GetRole(combatant.Job);
            IsTank = role == JobRole.Tank;
            IsMelee = role is JobRole.MeleeDps or JobRole.Tank;
            SpendsMana = role is JobRole.CasterDps or JobRole.Healer;
            ShieldsAllies = role is JobRole.Healer or JobRole.Tank;
        }

        public CombatantEntry Combatant { get; }
        public double BaseDps { get; }
        public double BaseHps { get; }
        public double CritChance { get; }
        public double DirectHitChance { get; }
        public float JoinedAt { get; }
        public bool IsTank { get; }
        public bool IsMelee { get; }
        public bool SpendsMana { get; }
        public bool ShieldsAllies { get; }

        public Dictionary<string, HitCounters> SkillCounters { get; } = new(StringComparer.Ordinal);

        public double QueuedDamage { get; set; }
        public double QueuedHeal { get; set; }
        public long PendingDamage { get; set; }
        public long PendingHeal { get; set; }
        public long GraphDamageTaken { get; set; }
        public int CritHeals { get; set; }
        public int HitsTaken { get; set; }
        public int Blocks { get; set; }
        public int Parries { get; set; }
        public float LastItemUse { get; set; }
    }

    private struct HitCounters
    {
        public int Crits;
        public int DirectHits;
        public int CritDirectHits;
    }

    private readonly struct ActiveBuff
    {
        public required StatusApplication Application { get; init; }
        public required float ExpiresAt { get; init; }
    }
}
