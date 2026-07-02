namespace DamageTerror.Services;

public sealed partial class SkillTracker
{
    public List<SkillUseEvent> GetSkillEvents(string combatantName)
    {
        lock (syncLock)
            return GetEventsWithFallback(skillEvents, seededEvents, combatantName);
    }

    public List<SkillUseEvent> GetDamageTakenEvents(string combatantName)
    {
        lock (syncLock)
            return GetEventsWithFallback(damageTakenEvents, seededDamageTakenEvents, combatantName);
    }

    public List<SkillUseEvent> GetItemEvents(string combatantName)
    {
        lock (syncLock)
            return GetEventsWithFallback(itemEvents, seededItemEvents, combatantName);
    }

    /// <summary>Returns a copy of live events, falling back to seeded historical events. Must be called under <see cref="syncLock"/>.</summary>
    private static List<SkillUseEvent> GetEventsWithFallback(
        Dictionary<string, List<SkillUseEvent>> live,
        Dictionary<string, List<SkillUseEvent>>? seeded,
        string combatantName)
    {
        if (live.TryGetValue(combatantName, out var events) && events.Count > 0)
            return new List<SkillUseEvent>(events);

        if (seeded != null
            && seeded.TryGetValue(combatantName, out var fallback)
            && fallback.Count > 0)
            return new List<SkillUseEvent>(fallback);

        return new List<SkillUseEvent>();
    }

    private void RecordEvent(string combatantName, string skillName, long amount, bool isHeal, byte severity,
        string? targetName = null, bool isDoTTick = false, bool isHoTTick = false)
    {
        if (!skillEvents.TryGetValue(combatantName, out var events))
            skillEvents[combatantName] = events = new List<SkillUseEvent>();

        events.Add(new SkillUseEvent
        {
            TimeSec = timer?.ElapsedSeconds ?? 0f,
            SkillName = skillName,
            TargetName = targetName,
            Amount = amount,
            IsHeal = isHeal,
            IsCrit = (severity & CritFlag) != 0,
            IsDirectHit = (severity & DirectHitFlag) != 0,
            IsDoTTick = isDoTTick,
            IsHoTTick = isHoTTick,
        });
    }

    /// <summary>
    /// Retroactively tag the most recent skill event as a DoT/HoT application.
    /// Called by StatusTracker when a GainsEffect (type 26) arrives for a known DoT/HoT.
    /// Sets IsDoTApplication/IsHoTApplication on the event for graph/timeline highlighting.
    /// </summary>
    public void MarkLastEventAsApplication(string combatantName, bool isDoT, bool isHoT)
    {
        lock (syncLock)
        {
            if (!skillEvents.TryGetValue(combatantName, out var events) || events.Count == 0)
                return;

            var now = timer?.ElapsedSeconds ?? 0f;
            const int maxScan = 10;
            const float maxAge = 3.0f;

            int start = Math.Max(0, events.Count - maxScan);
            for (int i = events.Count - 1; i >= start; i--)
            {
                var evt = events[i];

                if (evt.IsDoTTick || evt.IsHoTTick || evt.IsDoTApplication || evt.IsHoTApplication)
                    continue;

                if (now - evt.TimeSec > maxAge)
                    break;

                // Tag the first eligible event we find
                events[i] = evt with
                {
                    IsDoTApplication = isDoT || evt.IsDoTApplication,
                    IsHoTApplication = isHoT || evt.IsHoTApplication,
                };
                return;
            }
        }
    }

    private void RecordDamageTakenEvent(string targetName, string skillName, long amount, byte severity)
    {
        if (!damageTakenEvents.TryGetValue(targetName, out var events))
            damageTakenEvents[targetName] = events = new List<SkillUseEvent>();

        events.Add(new SkillUseEvent
        {
            TimeSec = timer?.ElapsedSeconds ?? 0f,
            SkillName = skillName,
            Amount = amount,
            IsHeal = false,
            IsCrit = (severity & CritFlag) != 0,
            IsDirectHit = (severity & DirectHitFlag) != 0,
        });
    }

    private void RecordItemEvent(string combatantName, string skillName, string? targetName)
    {
        if (!itemEvents.TryGetValue(combatantName, out var events))
            itemEvents[combatantName] = events = new List<SkillUseEvent>();

        events.Add(new SkillUseEvent
        {
            TimeSec = timer?.ElapsedSeconds ?? 0f,
            SkillName = skillName,
            TargetName = targetName,
            Amount = 0,
            IsHeal = false,
        });
    }
}
