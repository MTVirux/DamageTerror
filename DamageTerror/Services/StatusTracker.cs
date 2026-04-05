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
public class StatusTracker
{
    private readonly object syncLock = new();
    private readonly IDataManager dataManager;
    private readonly IPluginLog log;
    private EncounterTimer? timer;
    private SkillTracker? skillTracker;

    // (targetName, statusId) -> ActiveStatus
    // Multiple sources can apply the same status to the same target (e.g. two SCHs both apply Bio).
    // Key is (targetName, statusId, sourceName) to handle this.
    private readonly Dictionary<(string Target, uint StatusId, string Source), ActiveStatus> activeStatuses = new();

    // Historical record: all status applications this encounter (for uptime calculation)
    private readonly Dictionary<string, List<StatusApplication>> statusHistory = new(StringComparer.OrdinalIgnoreCase);

    // Historical record: statuses received by each target (keyed by target name)
    private readonly Dictionary<string, List<StatusApplication>> receivedHistory = new(StringComparer.OrdinalIgnoreCase);

    // Cache: statusId -> isDoT (true), isHoT, or neither
    private readonly ConcurrentDictionary<uint, StatusClassification> classificationCache = new();

    // Well-known DoT status IDs (FFXIV 7.x). Fallback when Lumina lookup is ambiguous.
    // These are the status effect IDs, NOT action IDs.
    private static readonly HashSet<uint> KnownDotStatusIds = new()
    {
        // Healer / White Mage
        0x74F, // Dia (1871)

        // Ranged / Bard
        1881, // Caustic Bite
        1200, // Stormbite

        // Caster / Summoner
        3089, // Slipstream (Garuda)

        // Healer / Scholar
        1895, // Biolysis
        179,  // Bio II (old but might still appear in older content)

        // Healer / Astrologian
        838,  // Combust II
        2041, // Combust III (7.x)

        // Tank / various
        248,  // Phlebotomize (legacy DRG)
        // Melee / Samurai
        1228, // Higanbana
    };

    private static readonly HashSet<uint> KnownHotStatusIds = new()
    {
        158,  // Regen (WHM)
        150,  // Medica II HoT
        1185, // Aspected Benefic (AST)
        835,  // Aspected Helios (AST)
        1874, // Whispering Dawn (SCH fairy)
        2618, // Sacred Soil HoT (SCH)
    };

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

    /// <summary>
    /// Process a GainsEffect event (ACT type 26).
    /// Fields: [0]=type, [1]=timestamp, [2]=statusId(hex), [3]=statusName,
    ///         [4]=duration(float), [5]=sourceId(hex), [6]=sourceName,
    ///         [7]=targetId(hex), [8]=targetName, [9]=stacks, ...
    /// </summary>
    public void OnStatusGained(string sourceName, string targetName, uint statusId, string statusName, float duration)
    {
        var classification = ClassifyStatus(statusId);
        var now = timer?.ElapsedSeconds ?? 0f;

        var status = new ActiveStatus
        {
            SourceName = sourceName,
            TargetName = targetName,
            StatusId = statusId,
            StatusName = statusName,
            AppliedAtSec = now,
            Duration = duration,
            IsDoT = classification.IsDoT,
            IsHoT = classification.IsHoT,
            IsBuff = classification.IsBuff,
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

        // Retroactively tag the type 21/22 event that applied this status
        // and capture the originating action name for DoT/HoT tick attribution.
        if (classification.IsDoT || classification.IsHoT)
        {
            var actionName = skillTracker?.MarkLastEventAsApplication(sourceName, classification.IsDoT, classification.IsHoT);
            if (!string.IsNullOrEmpty(actionName))
            {
                // Update the ActiveStatus with the resolved action name
                lock (syncLock)
                {
                    var key2 = (targetName, statusId, sourceName);
                    if (activeStatuses.TryGetValue(key2, out var s))
                    {
                        s.ApplyingActionName = actionName;
                        activeStatuses[key2] = s;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Process a LosesEffect event (ACT type 30).
    /// Fields: [0]=type, [1]=timestamp, [2]=statusId(hex), [3]=statusName,
    ///         [4]=duration, [5]=sourceId(hex), [6]=sourceName,
    ///         [7]=targetId(hex), [8]=targetName, ...
    /// </summary>
    public void OnStatusLost(string sourceName, string targetName, uint statusId, float removalTime)
    {
        lock (syncLock)
        {
            var key = (targetName, statusId, sourceName);
            if (activeStatuses.TryGetValue(key, out var existing))
            {
                RecordRemoval(existing, removalTime);
                activeStatuses.Remove(key);
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

    /// <summary>Get all currently active statuses on a target.</summary>
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

    /// <summary>Get the full status application history for a source player.</summary>
    public List<StatusApplication> GetStatusHistory(string sourceName)
    {
        lock (syncLock)
        {
            if (statusHistory.TryGetValue(sourceName, out var history))
                return new List<StatusApplication>(history);
            return new List<StatusApplication>();
        }
    }

    /// <summary>Get the full status received history for a target player (statuses applied TO them).</summary>
    public List<StatusApplication> GetStatusesReceived(string targetName)
    {
        lock (syncLock)
        {
            if (receivedHistory.TryGetValue(targetName, out var history))
                return new List<StatusApplication>(history);
            return new List<StatusApplication>();
        }
    }

    /// <summary>
    /// Calculate uptime percentage for a specific status applied by a source player
    /// across all targets. Returns 0-100.
    /// </summary>
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

                var endTime = app.RemovedAtSec ?? Math.Min(app.AppliedAtSec + app.Duration, encounterDuration);
                var activeTime = Math.Max(0f, endTime - app.AppliedAtSec);
                totalActiveTime += activeTime;
            }

            return Math.Min(100.0, totalActiveTime / encounterDuration * 100.0);
        }
    }

    /// <summary>Check whether a status ID is classified as a DoT.</summary>
    public bool IsDoT(uint statusId) => ClassifyStatus(statusId).IsDoT;

    /// <summary>Check whether a status ID is classified as a HoT.</summary>
    public bool IsHoT(uint statusId) => ClassifyStatus(statusId).IsHoT;

    public void Reset()
    {
        lock (syncLock)
        {
            activeStatuses.Clear();
            statusHistory.Clear();
            receivedHistory.Clear();
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

        var result = new StatusClassification();

        // Check hardcoded known sets first
        if (KnownDotStatusIds.Contains(statusId))
        {
            result.IsDoT = true;
            result.IsBuff = false;
        }
        else if (KnownHotStatusIds.Contains(statusId))
        {
            result.IsHoT = true;
            result.IsBuff = true;
        }

        // Attempt Lumina lookup to classify buff vs debuff
        try
        {
            var sheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Status>();
            if (sheet != null)
            {
                var row = sheet.GetRowOrDefault(statusId);
                if (row.HasValue)
                {
                    // StatusCategory: 1 = buff (beneficial), 2 = debuff (detrimental)
                    result.IsBuff = row.Value.StatusCategory == 1;
                }
            }
        }
        catch (Exception ex)
        {
            ServiceManager.PluginLog.Debug($"Failed to classify status {statusId}: {ex.Message}");
        }

        classificationCache[statusId] = result;
        return result;
    }

    private struct StatusClassification
    {
        public bool IsDoT;
        public bool IsHoT;
        public bool IsBuff;
    }
}
