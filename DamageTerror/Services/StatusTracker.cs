using System.Collections.Concurrent;
using System.Globalization;
using Dalamud.Plugin.Services;

namespace DamageTerror.Services;

/// <summary>
/// Tracks active status effects (buffs/debuffs) per target by processing
/// ACT log line types 26 (GainsEffect) and 30 (LosesEffect).
/// Used to correlate DoT/HoT ticks (type 24) back to the source player
/// and to compute uptime analytics.
/// </summary>
public sealed class StatusTracker
{
    public const float PermanentDurationThreshold = 9999f;

    private readonly object syncLock = new();
    private readonly IDataManager dataManager;
    private readonly IPluginLog log;
    private EncounterTimer? timer;
    private SkillTracker? skillTracker;

    // Multiple sources can apply the same status to the same target (e.g. two SCHs both apply Bio).
    private readonly Dictionary<(string Target, uint StatusId, string Source), ActiveStatus> activeStatuses = new();

    // Recently-removed DoT/HoT statuses, kept briefly so that type 24 ticks
    // arriving after a status-lost event (e.g. overwrite by another player) can
    // still be attributed to the correct source.
    private readonly List<ActiveStatus> recentlyRemovedDots = new();
    private const float RecentlyRemovedGraceSec = 6f; // ~2 server ticks

    private readonly Dictionary<string, List<StatusApplication>> statusHistory = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, List<StatusApplication>> receivedHistory = new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<uint, StatusClassification> classificationCache = new();

    private static readonly HashSet<uint> KnownDotStatusIds = JobRegistry.GetKnownDotStatusIds();

    private static readonly Dictionary<uint, string> GroundEffectDotIds = JobRegistry.GetGroundEffectDotIds();

    // Reverse map: skill name -> ground-effect status IDs (multiple IDs for PvE + PvP variants)
    private static readonly Dictionary<string, List<uint>> GroundEffectDotNameToIds =
        GroundEffectDotIds
            .GroupBy(kv => kv.Value, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToList(), StringComparer.OrdinalIgnoreCase);

    // Pending ground effects: skill was used (type 21/22) but status gain (type 26)
    // hasn't arrived yet. Ensures the first DoT tick is attributed correctly.
    private readonly Dictionary<(string Source, uint StatusId), float> pendingGroundEffects = new();
    private const float PendingGroundEffectTimeoutSec = 5f;

    private static readonly HashSet<uint> KnownHotStatusIds = JobRegistry.GetKnownHotStatusIds();

    public StatusTracker(IDataManager dataManager, IPluginLog log)
    {
        this.dataManager = dataManager;
        this.log = log;
    }

    public void SetTimer(EncounterTimer encounterTimer)
    {
        timer = encounterTimer;
    }

    public void SetSkillTracker(SkillTracker tracker)
    {
        skillTracker = tracker;
    }

    public float ElapsedSeconds => timer?.ElapsedSeconds ?? 0f;

    /// <summary>
    /// Pre-register a ground-effect DoT when the skill is used (type 21/22),
    /// before the status gain (type 26) arrives. This ensures the first tick
    /// is attributed correctly even if it arrives out of order.
    /// </summary>
    public void NotifyGroundEffectSkillUsed(string sourceName, string skillName)
    {
        if (!GroundEffectDotNameToIds.TryGetValue(skillName, out var statusIds))
            return;

        lock (syncLock)
        {
            var now = timer?.ElapsedSeconds ?? 0f;
            foreach (var statusId in statusIds)
                pendingGroundEffects[(sourceName, statusId)] = now;
        }
    }

    public void OnStatusGained(string sourceName, string targetName, uint statusId, string statusName, float duration,
        byte damageLowByte = 0, byte critLowByte = 0, bool hasLowByteData = false)
    {
        var classification = ClassifyStatus(statusId);
        var now = timer?.ElapsedSeconds ?? 0f;

        // Clear pending ground effect now that the real status has arrived.
        if (GroundEffectDotIds.ContainsKey(statusId))
        {
            lock (syncLock)
            {
                pendingGroundEffects.Remove((sourceName, statusId));
            }
        }

        var isPermanent = duration >= PermanentDurationThreshold;

        var status = new ActiveStatus
        {
            SourceName = sourceName,
            TargetName = targetName,
            StatusId = statusId,
            StatusName = statusName,
            AppliedAtSec = now,
            Duration = duration,
            IsPermanent = isPermanent,
            IsDoT = classification.IsDoT,
            IsHoT = classification.IsHoT,
            IsBuff = classification.IsBuff,
            DamageLowByte = damageLowByte,
            CritLowByte = critLowByte,
            HasLowByteData = hasLowByteData,
        };

        lock (syncLock)
        {
            var key = (targetName, statusId, sourceName);

            if (activeStatuses.TryGetValue(key, out var existing))
            {
                RecordRemoval(existing, now);
            }

            activeStatuses[key] = status;

            if (!statusHistory.TryGetValue(sourceName, out var history))
            {
                history = new List<StatusApplication>();
                statusHistory[sourceName] = history;
            }

            var application = new StatusApplication
            {
                StatusId = statusId,
                StatusName = statusName,
                SourceName = sourceName,
                TargetName = targetName,
                AppliedAtSec = now,
                Duration = duration,
                IsPermanent = isPermanent,
                IsDoT = classification.IsDoT,
                IsHoT = classification.IsHoT,
                IsBuff = classification.IsBuff,
            };

            history.Add(application);

            if (!receivedHistory.TryGetValue(targetName, out var received))
            {
                received = new List<StatusApplication>();
                receivedHistory[targetName] = received;
            }

            received.Add(application);
        }

        // Tag the originating skill event for graph/timeline highlighting
        // and set the applying action name for DoT/HoT tick attribution.
        // We use the status name directly as the skill label (following
        // xivanalysis's approach) rather than heuristic backwards-search.
        if (classification.IsDoT || classification.IsHoT)
        {
            skillTracker?.MarkLastEventAsApplication(sourceName, classification.IsDoT, classification.IsHoT);

            lock (syncLock)
            {
                var key2 = (targetName, statusId, sourceName);
                if (activeStatuses.TryGetValue(key2, out var s))
                {
                    s.ApplyingActionName = statusName;
                    activeStatuses[key2] = s;
                }
            }
        }
    }

    public void OnStatusLost(string sourceName, string targetName, uint statusId, float removalTime)
    {
        lock (syncLock)
        {
            var key = (targetName, statusId, sourceName);
            if (activeStatuses.TryGetValue(key, out var existing))
            {
                RecordRemoval(existing, removalTime);
                activeStatuses.Remove(key);

                // Keep DoT/HoT statuses in a grace-period buffer so that
                // type 24 ticks arriving after status removal can still be
                // attributed to the correct source.
                // Also keep ground-effect DoTs (e.g. Salted Earth) whose self-buff
                // status isn't classified as IsDoT but still needs tick attribution.
                if (existing.IsDoT || existing.IsHoT || GroundEffectDotIds.ContainsKey(existing.StatusId))
                {
                    existing.RemovedAtSec = removalTime;
                    recentlyRemovedDots.Add(existing);
                }
            }
        }
    }

    /// <summary>
    /// Look up who applied a given status to a target. Used by DoT tick
    /// attribution when type 24 lines lack explicit source info.
    /// </summary>
    public string? GetSourceForStatus(string targetName, uint statusId)
    {
        lock (syncLock)
        {
            ActiveStatus? best = null;
            foreach (var kv in activeStatuses)
            {
                if (kv.Key.Target == targetName && kv.Key.StatusId == statusId)
                {
                    if (best == null || kv.Value.AppliedAtSec > best.Value.AppliedAtSec)
                        best = kv.Value;
                }
            }

            return best?.SourceName;
        }
    }

    public List<ActiveStatus> GetActiveStatuses(string targetName)
    {
        lock (syncLock)
        {
            var result = new List<ActiveStatus>();
            foreach (var kv in activeStatuses)
            {
                if (string.Equals(kv.Key.Target, targetName, StringComparison.OrdinalIgnoreCase))
                    result.Add(kv.Value);
            }
            return result;
        }
    }

    public List<ActiveStatus> GetRecentlyRemovedDoTs(string targetName)
    {
        var now = timer?.ElapsedSeconds ?? 0f;
        lock (syncLock)
        {
            // Prune expired entries
            recentlyRemovedDots.RemoveAll(s => now - s.RemovedAtSec > RecentlyRemovedGraceSec);

            var result = new List<ActiveStatus>();
            foreach (var s in recentlyRemovedDots)
            {
                if (string.Equals(s.TargetName, targetName, StringComparison.OrdinalIgnoreCase))
                    result.Add(s);
            }
            return result;
        }
    }

    public List<StatusApplication> GetStatusHistory(string sourceName)
    {
        lock (syncLock)
        {
            if (statusHistory.TryGetValue(sourceName, out var history))
                return new List<StatusApplication>(history);
            return new List<StatusApplication>();
        }
    }

    public List<StatusApplication> GetStatusesReceived(string targetName)
    {
        lock (syncLock)
        {
            if (receivedHistory.TryGetValue(targetName, out var history))
                return new List<StatusApplication>(history);
            return new List<StatusApplication>();
        }
    }

    public double CalculateUptime(string sourceName, uint statusId, float encounterDuration)
    {
        if (encounterDuration <= 0f)
            return 0.0;

        lock (syncLock)
        {
            if (!statusHistory.TryGetValue(sourceName, out var history))
                return 0.0;

            float totalActiveTime = 0f;
            foreach (var app in history)
            {
                if (app.StatusId != statusId)
                    continue;

                var fallbackEnd = app.IsPermanent ? encounterDuration : Math.Min(encounterDuration, app.AppliedAtSec + app.Duration);
                var endTime = app.RemovedAtSec ?? fallbackEnd;
                var activeTime = Math.Max(0f, endTime - app.AppliedAtSec);
                totalActiveTime += activeTime;
            }

            return Math.Min(100.0, totalActiveTime / encounterDuration * 100.0);
        }
    }

    public bool IsDoT(uint statusId) => ClassifyStatus(statusId).IsDoT;
    public bool IsHoT(uint statusId) => ClassifyStatus(statusId).IsHoT;
    public bool IsGroundEffectDot(uint statusId) => GroundEffectDotIds.ContainsKey(statusId);

    /// <summary>
    /// Returns ground-effect DoT skill names for which the given source has an active self-buff.
    /// These are DoTs where the status is on the caster, not on the enemy target.
    /// </summary>
    public List<(string SkillName, uint StatusId)> GetActiveGroundEffectDots(string sourceName)
    {
        var now = timer?.ElapsedSeconds ?? 0f;
        var result = new List<(string, uint)>();
        lock (syncLock)
        {
            foreach (var (id, skillName) in GroundEffectDotIds)
            {
                // Ground-effect statuses are keyed as (target=source, statusId, source=source)
                // because ACT reports sourceName == targetName for self-buffs.
                var key = (sourceName, id, sourceName);
                if (activeStatuses.ContainsKey(key))
                {
                    result.Add((skillName, id));
                    continue;
                }

                // Pending: skill was used but status gain hasn't arrived yet.
                var pendingKey = (sourceName, id);
                if (pendingGroundEffects.TryGetValue(pendingKey, out var pendingTime)
                    && now - pendingTime <= PendingGroundEffectTimeoutSec)
                {
                    result.Add((skillName, id));
                    continue;
                }

                // Grace period: the self-buff may expire slightly before the last
                // DoT tick arrives. Check the recently-removed buffer.
                foreach (var s in recentlyRemovedDots)
                {
                    if (s.StatusId == id
                        && string.Equals(s.SourceName, sourceName, StringComparison.OrdinalIgnoreCase)
                        && now - s.RemovedAtSec <= RecentlyRemovedGraceSec)
                    {
                        result.Add((skillName, id));
                        break;
                    }
                }
            }
        }
        return result;
    }

    public void Reset()
    {
        lock (syncLock)
        {
            activeStatuses.Clear();
            recentlyRemovedDots.Clear();
            pendingGroundEffects.Clear();
            statusHistory.Clear();
            receivedHistory.Clear();
        }
    }

    /// <summary>
    /// Resets tracking state but carries forward any active DoT/HoT statuses
    /// so that the first tick of a new encounter is attributed correctly when
    /// the DoT was applied just before the encounter boundary.
    /// </summary>
    public void ResetKeepingActiveDoTs()
    {
        lock (syncLock)
        {
            // Snapshot DoT/HoT statuses (including ground-effect statuses)
            // before clearing everything.
            var carried = new List<(( string Target, uint StatusId, string Source) Key, ActiveStatus Status)>();
            foreach (var kv in activeStatuses)
            {
                if (kv.Value.IsDoT || kv.Value.IsHoT || GroundEffectDotIds.ContainsKey(kv.Value.StatusId))
                    carried.Add((kv.Key, kv.Value));
            }

            activeStatuses.Clear();
            recentlyRemovedDots.Clear();
            pendingGroundEffects.Clear();
            statusHistory.Clear();
            receivedHistory.Clear();

            // Re-inject with time reset to encounter start.
            foreach (var (key, status) in carried)
            {
                var restored = status;
                restored.AppliedAtSec = 0f;
                restored.RemovedAtSec = 0f;
                activeStatuses[key] = restored;
            }
        }
    }

    private void RecordRemoval(ActiveStatus status, float removalTime)
    {
        if (statusHistory.TryGetValue(status.SourceName, out var history))
        {
            for (int i = history.Count - 1; i >= 0; i--)
            {
                var app = history[i];
                if (app.StatusId == status.StatusId
                    && string.Equals(app.TargetName, status.TargetName, StringComparison.OrdinalIgnoreCase)
                    && app.RemovedAtSec == null)
                {
                    app.RemovedAtSec = removalTime;
                    history[i] = app;
                    break;
                }
            }
        }
    }

    private StatusClassification ClassifyStatus(uint statusId)
    {
        if (classificationCache.TryGetValue(statusId, out var cached))
            return cached;

        var isDoT = false;
        var isHoT = false;
        var isBuff = false;

        if (KnownDotStatusIds.Contains(statusId))
            isDoT = true;
        else if (KnownHotStatusIds.Contains(statusId))
        {
            isHoT = true;
            isBuff = true;
        }

        try
        {
            var sheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Status>();
            if (sheet != null)
            {
                var row = sheet.GetRowOrDefault(statusId);
                if (row.HasValue)
                    isBuff = row.Value.StatusCategory == 1;
            }
        }
        catch (Exception ex)
        {
            ServiceManager.PluginLog.Debug($"Failed to classify status {statusId}: {ex.Message}");
        }

        var result = new StatusClassification { IsDoT = isDoT, IsHoT = isHoT, IsBuff = isBuff };
        classificationCache[statusId] = result;
        return result;
    }

    private readonly struct StatusClassification
    {
        public bool IsDoT { get; init; }
        public bool IsHoT { get; init; }
        public bool IsBuff { get; init; }
    }
}
