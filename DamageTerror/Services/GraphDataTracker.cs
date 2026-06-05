namespace DamageTerror.Services;

using Dalamud.Plugin.Services;

public readonly struct GraphSample
{
    public required float TimeSec { get; init; }
    public required float Dps { get; init; }
    public required float Hps { get; init; }
    public required float Dtps { get; init; }
}

public struct ValidationStats
{
    public int CorrectionCount;
    public double MaxDivergence;
    public float LastCorrectionTime;
}

public sealed class GraphDataTracker
{
    private readonly object syncLock = new();
    private readonly IPluginLog? log;
    private readonly Dictionary<string, List<GraphSample>> perCombatant = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<(float time, long damage, long healed, long damageTaken)>> slidingWindowBuffer = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, (long damage, long healed)> logLineTotals = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (long damage, long healed, long damageTaken)> combatDataTotals = new(StringComparer.OrdinalIgnoreCase);

    private Dictionary<string, List<GraphSample>>? seededData;

    private EncounterTimer? timer;
    private float lastEmitTime;
    private bool previousWasActive;

    public ValidationStats Validation;

    /// <summary>Sliding window width in seconds for instantaneous DPS/HPS/DTPS calculation.</summary>
    public float WindowSeconds { get; set; } = 5f;
    /// <summary>Interval between emitted graph samples (controls graph resolution).</summary>
    public float SampleIntervalSeconds { get; set; } = 0.25f;
    /// <summary>Maximum allowed divergence (5%) between log-line and CombatData totals before applying correction.</summary>
    private const double ValidationThreshold = 0.05;

    public GraphDataTracker() { }

    public GraphDataTracker(IPluginLog log)
    {
        this.log = log;
    }

    public void SetTimer(EncounterTimer encounterTimer) => timer = encounterTimer;

    public void SeedHistorical(Dictionary<string, List<GraphSample>> data)
    {
        lock (syncLock)
        {
            seededData = new Dictionary<string, List<GraphSample>>(data, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Called from SkillTracker on every processed LogLine to feed the high-resolution
    /// damage/heal accumulator. Thread-safe.
    /// </summary>
    public void RecordLogLineEvent(string sourceName, long damageAmount, long healAmount)
    {
        if (string.IsNullOrEmpty(sourceName))
            return;

        lock (syncLock)
        {
            var existing = logLineTotals.GetValueOrDefault(sourceName);
            existing.damage += damageAmount;
            existing.healed += healAmount;
            logLineTotals[sourceName] = existing;
        }
    }

    public void RecordSample(EncounterSnapshot snapshot)
    {
        var enc = snapshot.Encounter;

        lock (syncLock)
        {
            if (enc.IsActive && !previousWasActive)
            {
                perCombatant.Clear();
                slidingWindowBuffer.Clear();
                logLineTotals.Clear();
                combatDataTotals.Clear();
                // Use a negative lastEmitTime so the very first frame always emits a sample.
                lastEmitTime = -SampleIntervalSeconds;
            }

            previousWasActive = enc.IsActive;

            if (!enc.IsActive)
            {
                // Trim graph to actual encounter length on first inactive frame
                if (timer != null && timer.IsRunning)
                {
                    timer.Stop();
                    var encDuration = DurationHelper.ParseDuration(enc.Duration);
                    if (encDuration > 0f)
                        TrimToEncounterLength(encDuration);
                }
                return;
            }

            var timeSec = timer?.ElapsedSeconds ?? 0f;

            foreach (var c in snapshot.Combatants)
            {
                if (string.IsNullOrEmpty(c.Name))
                    continue;
                combatDataTotals[c.Name] = (c.Damage, c.Healed, c.DamageTaken);
                ValidateAndCorrect(c.Name, c.Damage, c.Healed, timeSec);
            }

            if (timeSec - lastEmitTime < SampleIntervalSeconds)
                return;

            lastEmitTime = timeSec;
            var windowStart = timeSec - WindowSeconds;

            // Build effective totals per combatant: prefer LogLine data for damage/healed,
            // fall back to CombatData. Always use CombatData for damageTaken.
            foreach (var (name, cdTotals) in combatDataTotals)
            {
                var effectiveDamage = cdTotals.damage;
                var effectiveHealed = cdTotals.healed;

                if (logLineTotals.TryGetValue(name, out var llTotals))
                {
                    if (llTotals.damage > 0)
                        effectiveDamage = llTotals.damage;
                    if (llTotals.healed > 0)
                        effectiveHealed = llTotals.healed;
                }

                var effectiveDamageTaken = cdTotals.damageTaken;

                if (!perCombatant.TryGetValue(name, out var list))
                    perCombatant[name] = list = new List<GraphSample>();

                if (!slidingWindowBuffer.TryGetValue(name, out var history))
                    slidingWindowBuffer[name] = history = new List<(float, long, long, long)>();
                history.Add((timeSec, effectiveDamage, effectiveHealed, effectiveDamageTaken));

                while (history.Count > 2 && history[0].time < windowStart && history[1].time <= windowStart)
                    history.RemoveAt(0);

                float iDps = 0f, iHps = 0f, iDtps = 0f;
                var oldest = history[0];
                if (timeSec > oldest.time)
                {
                    var dt = (double)(timeSec - oldest.time);
                    iDps = Math.Max(0f, (float)((effectiveDamage - oldest.damage) / dt));
                    iHps = Math.Max(0f, (float)((effectiveHealed - oldest.healed) / dt));
                    iDtps = Math.Max(0f, (float)((effectiveDamageTaken - oldest.damageTaken) / dt));
                }

                list.Add(new GraphSample
                {
                    TimeSec = timeSec,
                    Dps = iDps,
                    Hps = iHps,
                    Dtps = iDtps,
                });
            }
        }
    }

    /// <summary>
    /// Compare LogLine-accumulated totals against CombatData ground truth.
    /// If the divergence exceeds <see cref="ValidationThreshold"/>, snap LogLine
    /// totals to the CombatData values and log a warning.
    /// </summary>
    private void ValidateAndCorrect(string name, long cdDamage, long cdHealed, float timeSec)
    {
        if (!logLineTotals.TryGetValue(name, out var ll))
            return; // No LogLine data yet — nothing to validate.

        bool corrected = false;
        double dmgDiv = 0, healDiv = 0;

        if (cdDamage > 0)
        {
            dmgDiv = Math.Abs(ll.damage - cdDamage) / (double)cdDamage;
            if (dmgDiv > ValidationThreshold)
                corrected = true;
        }

        if (cdHealed > 0)
        {
            healDiv = Math.Abs(ll.healed - cdHealed) / (double)cdHealed;
            if (healDiv > ValidationThreshold)
                corrected = true;
        }

        if (corrected)
        {
            var maxDiv = Math.Max(dmgDiv, healDiv);
            Validation.CorrectionCount++;
            if (maxDiv > Validation.MaxDivergence)
                Validation.MaxDivergence = maxDiv;
            Validation.LastCorrectionTime = timeSec;

            log?.Debug($"[GraphValidation] {name}: LogLine dmg {ll.damage} vs CombatData {cdDamage} " +
                       $"({dmgDiv:P1}), heal {ll.healed} vs {cdHealed} ({healDiv:P1}), correcting");

            logLineTotals[name] = (cdDamage, cdHealed);
        }
    }

    public List<GraphSample> GetSamples(string combatantName)
    {
        lock (syncLock)
        {
            if (perCombatant.TryGetValue(combatantName, out var list) && list.Count > 0)
                return new List<GraphSample>(list);

            // Fall back to seeded historical data when live tracker has nothing yet.
            if (seededData != null
                && seededData.TryGetValue(combatantName, out var seeded)
                && seeded.Count > 0)
                return new List<GraphSample>(seeded);

            return new List<GraphSample>();
        }
    }

    public void Reset()
    {
        lock (syncLock)
        {
            perCombatant.Clear();
            slidingWindowBuffer.Clear();
            logLineTotals.Clear();
            combatDataTotals.Clear();
            seededData = null;
            lastEmitTime = 0f;
            previousWasActive = false;
            Validation = default;
        }
    }

    private void TrimToEncounterLength(float encDuration)
    {
        foreach (var list in perCombatant.Values)
        {
            while (list.Count > 0 && list[^1].TimeSec > encDuration + 0.5f)
                list.RemoveAt(list.Count - 1);

            if (list.Count > 0)
            {
                var last = list[^1];
                if (last.TimeSec > encDuration)
                {
                    list[^1] = last with { TimeSec = encDuration };
                }
            }
        }
    }
}
