namespace DamageTerror.Services;

/// <summary>
/// Replays a saved encounter through the meter in real time. Walks the source
/// snapshot's timestamped event streams (SkillEvents, GraphData, status history,
/// etc.) forward against wall-clock × speed and rebuilds a working snapshot to
/// reflect the encounter state at the simulated time. The working snapshot is
/// what <see cref="EncounterStore"/> exposes as <c>active</c> while the replay
/// is loaded; the renderer reads it normally.
/// </summary>
public sealed class EncounterReplaySimulator
{
    private readonly EncounterSnapshot source;
    private readonly EncounterSnapshot working;
    private readonly float duration;

    private DateTime lastWallClock;
    private float simulatedTime;
    private float speed;
    private bool paused;
    private bool finished;

    private readonly Dictionary<string, int> skillCursor = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> damageTakenCursor = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> itemCursor = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> graphCursor = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> statusHistCursor = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> statusRecvCursor = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, Dictionary<string, RunningCounter>> dmgCounters = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, RunningCounter>> healCounters = new(StringComparer.OrdinalIgnoreCase);

    private readonly List<PendingExpiration> pendingExpirations = new();

    public EncounterReplaySimulator(EncounterSnapshot source, EncounterSnapshot working, float startSpeed = 1f)
    {
        this.source = source;
        this.working = working;
        this.duration = ComputeDuration(source);
        this.speed = Math.Clamp(startSpeed, 0.1f, 8f);
        this.lastWallClock = DateTime.UtcNow;
    }

    public bool IsRunning => !paused && !finished;
    public bool IsFinished => finished;
    public float ElapsedSeconds => simulatedTime;
    public float DurationSeconds => duration;

    public float Speed
    {
        get => speed;
        set => speed = Math.Clamp(value, 0.1f, 8f);
    }

    public void Pause()
    {
        if (finished) return;
        paused = true;
    }

    public void Resume()
    {
        if (finished)
            Seek(0f);
        paused = false;
        lastWallClock = DateTime.UtcNow;
    }

    public void Stop()
    {
        finished = true;
        working.Encounter.IsActive = false;
    }

    public void Seek(float targetSec)
    {
        targetSec = Math.Clamp(targetSec, 0f, duration);
        if (targetSec < simulatedTime - 0.0001f)
        {
            ResetWorkingState();
            simulatedTime = 0f;
        }
        AdvanceTo(targetSec);
        finished = simulatedTime >= duration - 0.001f;
        working.Encounter.IsActive = !finished;
        lastWallClock = DateTime.UtcNow;
    }

    public void Tick()
    {
        if (paused || finished)
        {
            lastWallClock = DateTime.UtcNow;
            return;
        }

        var now = DateTime.UtcNow;
        var dt = (float)(now - lastWallClock).TotalSeconds * speed;
        lastWallClock = now;
        if (dt <= 0f) return;

        var newTime = Math.Min(simulatedTime + dt, duration);
        AdvanceTo(newTime);

        if (newTime >= duration - 0.001f)
        {
            finished = true;
            working.Encounter.IsActive = false;
        }
    }

    private static float ComputeDuration(EncounterSnapshot src)
    {
        var d = DurationHelper.ParseDuration(src.Encounter.Duration, 0f);
        var maxEvent = 0f;
        foreach (var list in src.SkillEvents.Values)
            if (list.Count > 0) maxEvent = Math.Max(maxEvent, list[^1].TimeSec);
        foreach (var list in src.DamageTakenEvents.Values)
            if (list.Count > 0) maxEvent = Math.Max(maxEvent, list[^1].TimeSec);
        foreach (var list in src.ItemEvents.Values)
            if (list.Count > 0) maxEvent = Math.Max(maxEvent, list[^1].TimeSec);
        foreach (var list in src.GraphData.Values)
            if (list.Count > 0) maxEvent = Math.Max(maxEvent, list[^1].TimeSec);
        return Math.Max(0.5f, Math.Max(d, maxEvent));
    }

    private void ResetWorkingState()
    {
        working.ResetCombatStateForReplay();

        skillCursor.Clear();
        damageTakenCursor.Clear();
        itemCursor.Clear();
        graphCursor.Clear();
        statusHistCursor.Clear();
        statusRecvCursor.Clear();
        dmgCounters.Clear();
        healCounters.Clear();
        pendingExpirations.Clear();
    }

    private void AdvanceTo(float t)
    {
        foreach (var c in working.Combatants)
        {
            Advance(source.SkillEvents, working.SkillEvents, skillCursor, c.Name, e => e.TimeSec, t,
                (e, dst) => { dst.Add(e); ApplySkillEvent(c, e); });
            Advance(source.DamageTakenEvents, working.DamageTakenEvents, damageTakenCursor, c.Name, e => e.TimeSec, t,
                (e, dst) => { dst.Add(e); c.DamageTaken += e.Amount; });
            Advance(source.ItemEvents, working.ItemEvents, itemCursor, c.Name, e => e.TimeSec, t);
            Advance(source.GraphData, working.GraphData, graphCursor, c.Name, s => s.TimeSec, t);
            Advance(source.StatusHistory, working.StatusHistory, statusHistCursor, c.Name, s => s.AppliedAtSec, t,
                (s, dst) => EmitStatus(s, dst, t));
            Advance(source.StatusesReceived, working.StatusesReceived, statusRecvCursor, c.Name, s => s.AppliedAtSec, t,
                (s, dst) => EmitStatus(s, dst, t));
        }
        UpdatePendingExpirations(t);

        simulatedTime = t;

        long totalDmg = 0, totalHeal = 0, totalTaken = 0;
        foreach (var c in working.Combatants)
        {
            totalDmg += c.Damage;
            totalHeal += c.Healed;
            totalTaken += c.DamageTaken;
            c.CombatantDuration = SimulatorHelpers.FormatDuration(t);
            c.EncDps = t > 0 ? c.Damage / t : 0;
            c.EncHps = t > 0 ? c.Healed / t : 0;

            var (instDps, instHps) = ComputeInstantRates(c.Name, t);
            c.InstantDps = instDps;
            c.InstantHps = instHps;
            if (instDps > c.PeakDps) c.PeakDps = instDps;

            if (c.Hits > 0)
            {
                c.CritPct = (double)c.CritHitCount / c.Hits * 100.0;
                c.DirectHitPct = (double)c.DirectHitCount / c.Hits * 100.0;
                c.CritDirectHitPct = (double)c.CritDirectHitCount / c.Hits * 100.0;
            }
        }

        working.Encounter.TotalDamage = totalDmg;
        working.Encounter.TotalHealed = totalHeal;
        working.Encounter.EncDps = t > 0 ? totalDmg / (double)t : 0;
        working.Encounter.EncHps = t > 0 ? totalHeal / (double)t : 0;

        foreach (var c in working.Combatants)
        {
            c.DamagePercent = SimulatorHelpers.FormatPercent(c.Damage, totalDmg);
            c.HealedPercent = SimulatorHelpers.FormatPercent(c.Healed, totalHeal);
            c.DamageTakenPercent = SimulatorHelpers.FormatPercent(c.DamageTaken, totalTaken);
            c.RaidDps = working.Encounter.EncDps;
            c.RaidHps = working.Encounter.EncHps;
        }

        working.Encounter.Duration = SimulatorHelpers.FormatDuration(t);

        RebuildSkillEntries();
    }

    private void Advance<T>(
        IReadOnlyDictionary<string, List<T>> src,
        Dictionary<string, List<T>> dst,
        Dictionary<string, int> cursor,
        string name,
        Func<T, float> timeOf,
        float t,
        Action<T, List<T>>? onEmit = null)
    {
        if (!src.TryGetValue(name, out var s) || s.Count == 0) return;
        if (!cursor.TryGetValue(name, out var idx)) idx = 0;
        var d = dst.GetOrAdd(name);
        while (idx < s.Count && timeOf(s[idx]) <= t)
        {
            var e = s[idx];
            if (onEmit != null) onEmit(e, d);
            else d.Add(e);
            idx++;
        }
        cursor[name] = idx;
    }

    private void ApplySkillEvent(CombatantEntry c, SkillUseEvent e)
    {
        if (e.IsHeal)
        {
            c.Healed += e.Amount;
            UpdateCounter(healCounters, c.Name, e.SkillName, e);
            if (e.Amount > c.MaxHealAmount)
            {
                c.MaxHealAmount = e.Amount;
                c.MaxHeal = $"{e.SkillName}-{e.Amount}";
            }
        }
        else
        {
            c.Damage += e.Amount;
            UpdateCounter(dmgCounters, c.Name, e.SkillName, e);
            if (e.Amount > c.MaxHitDamage)
            {
                c.MaxHitDamage = e.Amount;
                c.MaxHit = $"{e.SkillName}-{e.Amount}";
            }
            c.Hits++;
            if (e.IsCrit) c.CritHitCount++;
            if (e.IsDirectHit) c.DirectHitCount++;
            if (e.IsCrit && e.IsDirectHit) c.CritDirectHitCount++;
        }
    }

    private void EmitStatus(StatusApplication s, List<StatusApplication> dst, float t)
    {
        var clone = CloneStatus(s, t);
        dst.Add(clone);
        if (s.RemovedAtSec.HasValue && s.RemovedAtSec.Value > t)
            pendingExpirations.Add(new PendingExpiration { Source = s, Clone = clone });
    }

    private void UpdatePendingExpirations(float t)
    {
        for (var i = pendingExpirations.Count - 1; i >= 0; i--)
        {
            var pe = pendingExpirations[i];
            if (pe.Source.RemovedAtSec.HasValue && pe.Source.RemovedAtSec.Value <= t)
            {
                pe.Clone.RemovedAtSec = pe.Source.RemovedAtSec;
                pendingExpirations.RemoveAt(i);
            }
        }
    }

    private static StatusApplication CloneStatus(StatusApplication s, float t)
        => new()
        {
            StatusId = s.StatusId,
            StatusName = s.StatusName,
            SourceName = s.SourceName,
            TargetName = s.TargetName,
            AppliedAtSec = s.AppliedAtSec,
            Duration = s.Duration,
            RemovedAtSec = s.RemovedAtSec.HasValue && s.RemovedAtSec.Value <= t ? s.RemovedAtSec : null,
            IsPermanent = s.IsPermanent,
            IsDoT = s.IsDoT,
            IsHoT = s.IsHoT,
            IsBuff = s.IsBuff,
        };

    private (double dps, double hps) ComputeInstantRates(string name, float t)
    {
        if (working.GraphData.TryGetValue(name, out var samples) && samples.Count > 0)
        {
            var last = samples[^1];
            return (last.Dps, last.Hps);
        }

        if (!working.SkillEvents.TryGetValue(name, out var events) || events.Count == 0)
            return (0, 0);
        const float window = 5f;
        var lo = t - window;
        long dmg = 0, heal = 0;
        for (var i = events.Count - 1; i >= 0; i--)
        {
            var e = events[i];
            if (e.TimeSec < lo) break;
            if (e.IsHeal) heal += e.Amount; else dmg += e.Amount;
        }
        return (dmg / window, heal / window);
    }

    private static void UpdateCounter(
        Dictionary<string, Dictionary<string, RunningCounter>> store,
        string combatantName, string skillName, SkillUseEvent e)
    {
        if (!store.TryGetValue(combatantName, out var perSkill))
            store[combatantName] = perSkill = new Dictionary<string, RunningCounter>();
        var rc = perSkill.GetValueOrDefault(skillName);
        rc.Total += e.Amount;
        rc.Hits++;
        if (e.IsCrit && e.IsDirectHit) rc.CritDirectHits++;
        else if (e.IsCrit) rc.Crits++;
        else if (e.IsDirectHit) rc.DirectHits++;
        perSkill[skillName] = rc;
    }

    private void RebuildSkillEntries()
    {
        foreach (var c in working.Combatants)
        {
            c.Skills = BuildEntries(dmgCounters, c.Name);
            c.HealingSkills = BuildEntries(healCounters, c.Name);
        }
    }

    private static List<SkillEntry> BuildEntries(
        Dictionary<string, Dictionary<string, RunningCounter>> store, string combatantName)
    {
        var list = new List<SkillEntry>();
        if (!store.TryGetValue(combatantName, out var perSkill) || perSkill.Count == 0)
            return list;

        long total = 0;
        foreach (var rc in perSkill.Values) total += rc.Total;

        foreach (var (name, rc) in perSkill)
        {
            var entry = new SkillEntry
            {
                Name = name,
                TotalDamage = rc.Total,
                HitCount = rc.Hits,
            };
            if (rc.Hits > 0)
                entry.SetHitRates(rc.Crits, rc.DirectHits, rc.CritDirectHits, rc.Hits);
            if (total > 0)
                entry.DamagePercent = (double)rc.Total / total * 100.0;
            list.Add(entry);
        }
        list.Sort((a, b) => b.TotalDamage.CompareTo(a.TotalDamage));
        return list;
    }

    private struct RunningCounter
    {
        public long Total;
        public int Hits;
        public int Crits;
        public int DirectHits;
        public int CritDirectHits;
    }

    private struct PendingExpiration
    {
        public StatusApplication Source;
        public StatusApplication Clone;
    }
}
