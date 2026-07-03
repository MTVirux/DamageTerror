namespace DamageTerror.Services;


/// <summary>
/// Mutates a sample EncounterSnapshot in-place each tick to simulate active combat.
/// </summary>
internal sealed class SampleCombatSimulator
{
    private static Random Rng => Random.Shared;

    private readonly EncounterSnapshot snapshot;
    private readonly List<double> baseDps = new();
    private readonly List<double> baseHps = new();
    private readonly DateTime startTime;
    private float lastGraphSampleTime;
    private float lastSkillEventTime;
    private float lastStatusTickTime;
    private float lastSpawnTime;

    private readonly Func<CombatantEntry?>? combatantFactory;
    private readonly Dictionary<string, List<ActiveBuff>> activeBuffs = new(StringComparer.OrdinalIgnoreCase);

    public bool IsRunning { get; private set; }

    public SampleCombatSimulator(EncounterSnapshot snapshot, Func<CombatantEntry?>? combatantFactory = null)
    {
        this.snapshot = snapshot;
        this.startTime = DateTime.UtcNow;
        this.combatantFactory = combatantFactory;

        foreach (var c in snapshot.Combatants)
        {
            baseDps.Add(c.EncDps);
            baseHps.Add(c.EncHps);
        }

        snapshot.Encounter.IsActive = true;
        snapshot.Encounter.Duration = "00:00";

        foreach (var c in snapshot.Combatants)
        {
            c.Damage = 0;
            c.Healed = 0;
        }
        snapshot.Encounter.TotalDamage = 0;
        snapshot.Encounter.TotalHealed = 0;

        snapshot.GraphData.Clear();
        snapshot.SkillEvents.Clear();
        snapshot.DamageTakenEvents.Clear();
        snapshot.StatusHistory.Clear();
        snapshot.StatusesReceived.Clear();

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

        if (combatantFactory != null && elapsed - lastSpawnTime >= 0.5f)
        {
            lastSpawnTime = elapsed;
            var newCombatant = combatantFactory();
            if (newCombatant != null)
            {
                newCombatant.Damage = 0;
                newCombatant.Healed = 0;
                snapshot.Combatants.Add(newCombatant);
                baseDps.Add(newCombatant.EncDps);
                baseHps.Add(newCombatant.EncHps);
            }
        }

        long totalDamage = 0;
        long totalHealed = 0;

        for (var i = 0; i < snapshot.Combatants.Count; i++)
        {
            var c = snapshot.Combatants[i];
            var bdps = baseDps[i];
            var bhps = baseHps[i];

            var wave = 1.0 + 0.15 * Math.Sin(elapsed * (0.3 + i * 0.07));
            var noise = 0.9 + Rng.NextDouble() * 0.2;
            var instantDps = bdps * wave * noise;
            var instantHps = bhps * (1.0 + 0.2 * Math.Sin(elapsed * (0.25 + i * 0.05))) * (0.85 + Rng.NextDouble() * 0.3);

            c.Damage = (long)(bdps * elapsed * (0.95 + 0.1 * Math.Sin(elapsed * 0.1 + i)));
            c.Healed = (long)(bhps * elapsed * (0.95 + 0.1 * Math.Sin(elapsed * 0.08 + i)));

            c.EncDps = elapsed > 0 ? c.Damage / (double)elapsed : 0;
            c.EncHps = elapsed > 0 ? c.Healed / (double)elapsed : 0;
            c.InstantDps = instantDps;
            c.InstantHps = instantHps;
            c.PeakDps = Math.Max(c.PeakDps, instantDps);

            totalDamage += c.Damage;
            totalHealed += c.Healed;
        }

        snapshot.Encounter.TotalDamage = totalDamage;
        snapshot.Encounter.TotalHealed = totalHealed;
        snapshot.Encounter.EncDps = elapsed > 0 ? totalDamage / (double)elapsed : 0;
        snapshot.Encounter.EncHps = elapsed > 0 ? totalHealed / (double)elapsed : 0;

        foreach (var c in snapshot.Combatants)
        {
            c.DamagePercent = SimulatorHelpers.FormatPercent(c.Damage, totalDamage);
            c.HealedPercent = SimulatorHelpers.FormatPercent(c.Healed, totalHealed);
        }

        if (elapsed - lastGraphSampleTime >= 0.5f)
        {
            lastGraphSampleTime = elapsed;

            for (var i = 0; i < snapshot.Combatants.Count; i++)
            {
                var c = snapshot.Combatants[i];
                var samples = snapshot.GraphData.GetOrAdd(c.Name);

                samples.Add(new GraphSample
                {
                    TimeSec = elapsed,
                    Dps = (float)c.InstantDps,
                    Hps = (float)c.InstantHps,
                    Dtps = (float)(Rng.NextDouble() * 2000),
                });
            }
        }

        if (elapsed - lastSkillEventTime >= 0.8f)
        {
            lastSkillEventTime = elapsed;
            TickSkillEvents(elapsed);
        }

        if (elapsed - lastStatusTickTime >= 2.0f)
        {
            lastStatusTickTime = elapsed;
            TickStatuses(elapsed);
        }
    }

    private void TickSkillEvents(float elapsed)
    {
        for (var i = 0; i < snapshot.Combatants.Count; i++)
        {
            var c = snapshot.Combatants[i];

            if (Rng.NextDouble() > 0.6) continue;

            var isHeal = c.EncHps > 1000 && (c.Skills.Count == 0 || Rng.NextDouble() < 0.4);
            var skillList = isHeal ? c.HealingSkills : c.Skills;
            if (skillList.Count == 0) continue;

            var skill = skillList[Rng.Next(skillList.Count)];
            var amount = (long)(Rng.Next(5000, 80000) * (isHeal ? 0.6 : 1.0));
            var isCrit = Rng.NextDouble() < c.CritPct / 100.0;
            var isDh = Rng.NextDouble() < c.DirectHitPct / 100.0;

            var evt = new SkillUseEvent
            {
                TimeSec = elapsed,
                SkillName = skill.Name,
                Amount = amount,
                IsHeal = isHeal,
                IsCrit = isCrit,
                IsDirectHit = isDh,
                IsDoTTick = !isHeal && skill.DamageType == SkillDamageType.Magic && Rng.NextDouble() < 0.15,
            };

            snapshot.SkillEvents.GetOrAdd(c.Name).Add(evt);

            skill.TotalDamage += amount;
            skill.HitCount++;

            if (!isHeal && Rng.NextDouble() < 0.15)
            {
                var dtEvt = new SkillUseEvent
                {
                    TimeSec = elapsed,
                    SkillName = GetRandomBossSkill(),
                    Amount = Rng.Next(10000, 60000),
                    IsHeal = false,
                    IsCrit = false,
                };

                snapshot.DamageTakenEvents.GetOrAdd(c.Name).Add(dtEvt);
            }
        }
    }

    private void TickStatuses(float elapsed)
    {
        for (var i = 0; i < snapshot.Combatants.Count; i++)
        {
            var c = snapshot.Combatants[i];

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
                {
                    var target = snapshot.Combatants[Rng.Next(snapshot.Combatants.Count)];
                    app.TargetName = target.Name;
                }

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

    private static string GetRandomBossSkill()
    {
        var skills = new[]
        {
            "Akh Morn", "Megaflare", "Exaflare", "Diamond Dust", "Earthen Fury",
            "Hellfire", "Judgment Bolt", "Tidal Wave", "Aerial Blast", "Cauterize",
            "Tera Slash", "Giga Slash", "Wave Cannon", "Ion Efflux", "Atomic Ray",
        };
        return skills[Rng.Next(skills.Length)];
    }

    public void Stop()
    {
        IsRunning = false;
        snapshot.Encounter.IsActive = false;
    }

    private readonly struct ActiveBuff
    {
        public required StatusApplication Application { get; init; }
        public required float ExpiresAt { get; init; }
    }
}
