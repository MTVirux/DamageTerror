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
    public const float PermanentDurationThreshold = 9999f;

    private readonly object syncLock = new();
    private readonly IDataManager dataManager;
    private readonly IPluginLog log;
    private EncounterTimer? timer;
    private SkillTracker? skillTracker;

    // (targetName, statusId) -> ActiveStatus
    // Multiple sources can apply the same status to the same target (e.g. two SCHs both apply Bio).
    // Key is (targetName, statusId, sourceName) to handle this.
    private readonly Dictionary<(string Target, uint StatusId, string Source), ActiveStatus> activeStatuses = new();

    // Recently-removed DoT/HoT statuses, kept briefly so that type 24 ticks
    // arriving after a status-lost event (e.g. overwrite by another player) can
    // still be attributed to the correct source.
    private readonly List<ActiveStatus> recentlyRemovedDots = new();
    private const float RecentlyRemovedGraceSec = 6f; // ~2 server ticks

    // Historical record: all status applications this encounter (for uptime calculation)
    private readonly Dictionary<string, List<StatusApplication>> statusHistory = new(StringComparer.OrdinalIgnoreCase);

    // Historical record: statuses received by each target (keyed by target name)
    private readonly Dictionary<string, List<StatusApplication>> receivedHistory = new(StringComparer.OrdinalIgnoreCase);

    // Cache: statusId -> isDoT (true), isHoT, or neither
    private readonly ConcurrentDictionary<uint, StatusClassification> classificationCache = new();

    // Well-known DoT status IDs (FFXIV 7.x). Fallback when Lumina lookup is ambiguous.
    // These are the status effect IDs, NOT action IDs.
    // Verified against xivanalysis data (src/data/STATUSES/root/*.ts).
    private static readonly HashSet<uint> KnownDotStatusIds = new()
    {
        // Healer / White Mage
        1871, // Dia
        143,  // Aero
        144,  // Aero II
        798,  // Aero III

        // Ranged / Bard
        124,  // Venomous Bite
        129,  // Windbite
        1200, // Caustic Bite
        1201, // Stormbite

        // Caster / Summoner
        2706, // Slipstream (Garuda)

        // Healer / Scholar
        1895, // Biolysis
        189,  // Bio II

        // Healer / Astrologian
        838,  // Combust
        843,  // Combust II
        1881, // Combust III

        // Healer / Sage
        2614, // Eukrasian Dosis
        2615, // Eukrasian Dosis II
        2616, // Eukrasian Dosis III
        3897, // Eukrasian Dyskrasia

        // Melee / Samurai
        1228, // Higanbana

        // Melee / Dragoon
        118,  // Chaos Thrust
        2719, // Chaotic Spring

        // Melee / Viper
        3667, // Noxious Gnash

        // Caster / Blue Mage
        1714, // Bleeding (Song of Torment, Nightbloom, Aetherial Spark)
        1736, // Dropsy (Aqua Breath)
        18,   // Poison (Bad Breath)
        1723, // Windburn (Feather Rain)
        3712, // Breath of Magic
        3643, // Mortal Flame
    };

    private static readonly HashSet<uint> KnownHotStatusIds = new()
    {
        // White Mage
        158,  // Regen
        150,  // Medica II
        3880, // Medica III

        // Astrologian
        835,  // Aspected Benefic
        836,  // Aspected Helios
        3894, // Helios Conjunction

        // Scholar
        315,  // Whispering Dawn
        1874, // Angel's Whisper (Seraph)
        1944, // Sacred Soil
        3885, // Seraphism HoT

        // Sage
        2617, // Physis
        2620, // Physis II
        2938, // Kerakeia
        3898, // Philosophia

        // Blue Mage
        2495, // Angel's Snack
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

    public float ElapsedSeconds => timer?.ElapsedSeconds ?? 0f;

    public void OnStatusGained(string sourceName, string targetName, uint statusId, string statusName, float duration)
    {
        var classification = ClassifyStatus(statusId);
        var now = timer?.ElapsedSeconds ?? 0f;

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
                if (existing.IsDoT || existing.IsHoT)
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

    public void Reset()
    {
        lock (syncLock)
        {
            activeStatuses.Clear();
            recentlyRemovedDots.Clear();
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
