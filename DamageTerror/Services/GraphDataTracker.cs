namespace DamageTerror.Services;

using System.Diagnostics;

public struct GraphSample
{
    public float TimeSec;
    public float Dps;
    public float Hps;
    public float Dtps;
}

public class GraphDataTracker
{
    private readonly object syncLock = new();
    private readonly Dictionary<string, List<GraphSample>> perCombatant = new(StringComparer.OrdinalIgnoreCase);
    // Ring buffer of recent (time, totals) per combatant for sliding window
    private readonly Dictionary<string, List<(float time, long damage, long healed, long damageTaken)>> recentHistory = new(StringComparer.OrdinalIgnoreCase);
    // Latest received totals (updated every data frame)
    private readonly Dictionary<string, (long damage, long healed, long damageTaken)> latestTotals = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stopwatch stopwatch = new();
    private float lastEmitTime;
    private bool lastWasActive;

    /// <summary>Sliding window size in seconds for rate smoothing.</summary>
    public float WindowSeconds { get; set; } = 5f;

    public void RecordSample(EncounterSnapshot snapshot)
    {
        var enc = snapshot.Encounter;

        lock (syncLock)
        {
            // Reset when a new encounter starts
            if (enc.IsActive && !lastWasActive)
            {
                perCombatant.Clear();
                recentHistory.Clear();
                latestTotals.Clear();
                stopwatch.Restart();
                lastEmitTime = 0f;
            }

            lastWasActive = enc.IsActive;

            // Only record while the encounter is active
            if (!enc.IsActive)
            {
                // Trim graph to actual encounter length on first inactive frame
                if (stopwatch.IsRunning)
                {
                    stopwatch.Stop();
                    var encDuration = ParseDuration(enc.Duration);
                    if (encDuration > 0f)
                        TrimToEncounterLength(encDuration);
                }
                return;
            }

            var timeSec = (float)stopwatch.Elapsed.TotalSeconds;

            // Always update latest totals from incoming data
            foreach (var c in snapshot.Combatants)
            {
                if (string.IsNullOrEmpty(c.Name))
                    continue;
                latestTotals[c.Name] = (c.Damage, c.Healed, c.DamageTaken);
            }

            // Only emit graph points once per second
            if (timeSec - lastEmitTime < 1f)
                return;

            lastEmitTime = timeSec;
            var windowStart = timeSec - WindowSeconds;

            foreach (var (name, totals) in latestTotals)
            {
                if (!perCombatant.TryGetValue(name, out var list))
                {
                    list = new List<GraphSample>();
                    perCombatant[name] = list;
                }

                // Add current snapshot to history
                if (!recentHistory.TryGetValue(name, out var history))
                {
                    history = new List<(float, long, long, long)>();
                    recentHistory[name] = history;
                }
                history.Add((timeSec, totals.damage, totals.healed, totals.damageTaken));

                // Trim entries older than the window (keep at least one old entry as anchor)
                while (history.Count > 2 && history[0].time < windowStart && history[1].time <= windowStart)
                    history.RemoveAt(0);

                // Compute rate over the sliding window
                float iDps = 0f, iHps = 0f, iDtps = 0f;
                var oldest = history[0];
                if (timeSec > oldest.time)
                {
                    var dt = (double)(timeSec - oldest.time);
                    iDps = Math.Max(0f, (float)((totals.damage - oldest.damage) / dt));
                    iHps = Math.Max(0f, (float)((totals.healed - oldest.healed) / dt));
                    iDtps = Math.Max(0f, (float)((totals.damageTaken - oldest.damageTaken) / dt));
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

    public List<GraphSample> GetSamples(string combatantName)
    {
        lock (syncLock)
        {
            if (perCombatant.TryGetValue(combatantName, out var list))
                return new List<GraphSample>(list);
            return new List<GraphSample>();
        }
    }

    public void Reset()
    {
        lock (syncLock)
        {
            perCombatant.Clear();
            recentHistory.Clear();
            latestTotals.Clear();
            stopwatch.Reset();
            lastEmitTime = 0f;
            lastWasActive = false;
        }
    }

    /// <summary>Remove samples beyond the encounter duration and cap the last sample's time.</summary>
    private void TrimToEncounterLength(float encDuration)
    {
        foreach (var list in perCombatant.Values)
        {
            // Remove samples that are past the encounter duration
            while (list.Count > 0 && list[^1].TimeSec > encDuration + 0.5f)
                list.RemoveAt(list.Count - 1);

            // Cap the last sample's time to the encounter duration
            if (list.Count > 0)
            {
                var last = list[^1];
                if (last.TimeSec > encDuration)
                {
                    last.TimeSec = encDuration;
                    list[^1] = last;
                }
            }
        }
    }

    private static float ParseDuration(string duration)
    {
        if (string.IsNullOrEmpty(duration))
            return 0f;

        var parts = duration.Split(':');
        if (parts.Length == 2
            && float.TryParse(parts[0], out var mins)
            && float.TryParse(parts[1], out var secs))
            return mins * 60f + secs;

        if (parts.Length == 3
            && float.TryParse(parts[0], out var hrs)
            && float.TryParse(parts[1], out var m2)
            && float.TryParse(parts[2], out var s2))
            return hrs * 3600f + m2 * 60f + s2;

        return 0f;
    }
}
