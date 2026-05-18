using Dalamud.Plugin.Services;
using Newtonsoft.Json;

namespace DamageTerror.Services;

public sealed class EncounterStore
{
    private readonly object syncLock = new();
    private readonly List<EncounterSnapshot> history = new();
    private readonly Configuration config;
    private EncounterSnapshot? active;
    private bool prevSnapshotActive;
    /// <summary>When true, drop incoming CombatData until a genuinely new encounter starts.
    /// Set after the user manually removes the active encounter via RemoveActive().</summary>
    private bool isStaleDataSuppressed;
    private bool activeAlreadyInHistory;
    private string? savePath;
    private TimelineSidecarStore? timelineStore;
    private bool dirty;
    private bool loadedSuccessfully;
    private bool sampleDataActive;
    private EncounterSnapshot? previewBackup;
    private SampleCombatSimulator? sampleSimulator;
    private EncounterReplaySimulator? replaySimulator;
    private Func<CombatantEntry?>? pendingFactory;
    /// <summary>Tracks the IsActive state of the most recent incoming snapshot,
    /// regardless of sampleDataActive gating. Used to detect a fresh live encounter
    /// starting during replay so the meter can yield to live combat.</summary>
    private bool lastIncomingActive;

    public EncounterStore(Configuration config)
    {
        this.config = config;
    }

    public long StorageSizeBytes
    {
        get
        {
            if (string.IsNullOrEmpty(savePath))
                return 0;
            try
            {
                var info = new System.IO.FileInfo(savePath);
                return info.Exists ? info.Length : 0;
            }
            catch { return 0; }
        }
    }

    public long TimelineStorageSizeBytes => timelineStore?.TotalSizeBytes() ?? 0;
    public int TimelineFileCount => timelineStore?.FileCount() ?? 0;
    public string? TimelineDirectory => timelineStore?.DirectoryPath;

    public EncounterSnapshot? ActiveEncounter
    {
        get { lock (syncLock) return active; }
    }

    public List<EncounterSnapshot> History
    {
        get
        {
            lock (syncLock)
                return new List<EncounterSnapshot>(history);
        }
    }

    public int TotalCount
    {
        get
        {
            lock (syncLock)
                return history.Count + (active != null ? 1 : 0);
        }
    }

    public EncounterSnapshot? GetByIndex(int index)
    {
        EncounterSnapshot? snapshot;
        lock (syncLock)
        {
            if (index < 0) return null;
            if (index < history.Count) snapshot = history[index];
            else if (index == history.Count && active != null) snapshot = active;
            else return null;
        }
        EnsureTimelineLoaded(snapshot);
        return snapshot;
    }

    public bool IsSampleDataActive
    {
        get { lock (syncLock) return sampleDataActive; }
    }

    public bool IsSampleSimulating
    {
        get { lock (syncLock) return sampleSimulator?.IsRunning ?? false; }
    }

    public bool IsReplayActive
    {
        get { lock (syncLock) return replaySimulator != null; }
    }

    public EncounterReplaySimulator? ReplaySimulator
    {
        get { lock (syncLock) return replaySimulator; }
    }

    public event Action? OnSampleDataLoaded;

    public void LoadSampleData(EncounterSnapshot sample, bool simulate = false, Func<CombatantEntry?>? combatantFactory = null)
    {
        lock (syncLock)
        {
            sampleSimulator?.Stop();
            sampleSimulator = null;
            replaySimulator?.Stop();
            replaySimulator = null;

            if (!sampleDataActive)
                previewBackup = active;
            sampleDataActive = true;
            active = sample;
            pendingFactory = combatantFactory;

            if (simulate)
                sampleSimulator = new SampleCombatSimulator(sample, combatantFactory);
        }

        OnSampleDataLoaded?.Invoke();
    }

    /// <summary>
    /// Begin replaying a finished encounter. Source is left untouched; the meter
    /// shows a fresh working clone that the simulator mutates each tick to
    /// reflect the encounter state at the simulated time.
    /// </summary>
    public void LoadReplay(EncounterSnapshot source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        EnsureTimelineLoaded(source);

        // Make sure structural events exist (older saves that pre-date SkillEvents
        // capture rely on synthesis from per-combatant Skills aggregates).
        source.ValidateAndRepair();

        var working = CloneSnapshotShellForReplay(source);

        lock (syncLock)
        {
            sampleSimulator?.Stop();
            sampleSimulator = null;
            replaySimulator?.Stop();
            replaySimulator = null;
            pendingFactory = null;

            if (!sampleDataActive)
                previewBackup = active;
            sampleDataActive = true;
            active = working;
            replaySimulator = new EncounterReplaySimulator(source, working, 1f);
        }

        OnSampleDataLoaded?.Invoke();
    }

    private static EncounterSnapshot CloneSnapshotShellForReplay(EncounterSnapshot source)
    {
        // JSON round-trip gives a full deep clone. One-shot cost on replay start.
        var json = JsonConvert.SerializeObject(source);
        var clone = JsonConvert.DeserializeObject<EncounterSnapshot>(json)
            ?? throw new InvalidOperationException("Failed to clone encounter for replay.");
        clone.ResetCombatStateForReplay();
        return clone;
    }

    public void SetSampleSimulation(bool enabled)
    {
        lock (syncLock)
        {
            if (!sampleDataActive || active == null) return;

            if (enabled && sampleSimulator?.IsRunning != true)
                sampleSimulator = new SampleCombatSimulator(active, pendingFactory);
            else if (!enabled)
            {
                sampleSimulator?.Stop();
                sampleSimulator = null;
            }
        }
    }

    public void TickSampleSimulation()
    {
        lock (syncLock)
        {
            sampleSimulator?.Tick();
            replaySimulator?.Tick();
        }
    }

    public void ClearSampleData()
    {
        lock (syncLock)
        {
            if (!sampleDataActive) return;
            sampleSimulator?.Stop();
            sampleSimulator = null;
            replaySimulator?.Stop();
            replaySimulator = null;
            pendingFactory = null;
            sampleDataActive = false;
            active = previewBackup;
            previewBackup = null;
        }
    }

    public bool Update(EncounterSnapshot snapshot)
    {
        lock (syncLock)
        {
            var incomingActive = snapshot.Encounter.IsActive;
            var newFightStarting = incomingActive && !lastIncomingActive;
            lastIncomingActive = incomingActive;

            if (sampleDataActive)
            {
                // Replay yields to live combat when a fresh fight starts. Sample
                // data stays sticky (its lifecycle is explicitly user-controlled).
                if (replaySimulator != null && newFightStarting)
                {
                    replaySimulator.Stop();
                    replaySimulator = null;
                    pendingFactory = null;
                    sampleDataActive = false;
                    active = previewBackup;
                    previewBackup = null;
                    // Treat the previous active as no-longer-live so the archive
                    // branch below closes it out before the new fight begins.
                    prevSnapshotActive = false;
                }
                else
                {
                    return false;
                }
            }

            var archived = false;

            if (isStaleDataSuppressed)
            {
                if (snapshot.Encounter.IsActive && !prevSnapshotActive)
                    isStaleDataSuppressed = false;
                else
                {
                    prevSnapshotActive = snapshot.Encounter.IsActive;
                    return false;
                }
            }

            if (snapshot.Encounter.IsActive && !prevSnapshotActive && active != null)
            {
                active.Encounter.IsActive = false;
                if (!activeAlreadyInHistory && !double.IsNaN(active.Encounter.EncDps)
                    && !(config.SkipZeroEdpsEncounters && active.Encounter.EncDps == 0))
                {
                    AssignIdIfMissing(active);
                    history.Add(active);
                    SaveTimelineSidecarLocked(active);
                    dirty = true;
                    archived = true;
                    PruneHistoryLocked();
                }
                activeAlreadyInHistory = false;
            }
            else if (!snapshot.Encounter.IsActive && !prevSnapshotActive && active != null
                     && active != snapshot
                     && (active.GraphData.Count > 0 || active.SkillEvents.Count > 0))
            {
                // The active encounter was restored from history and has persisted
                // graph/skill data. Carry the data forward to the incoming snapshot
                // instead of archiving (which would create a duplicate on reload).
                foreach (var kvp in active.GraphData)
                {
                    if (!snapshot.GraphData.ContainsKey(kvp.Key))
                        snapshot.GraphData[kvp.Key] = kvp.Value;
                }

                foreach (var kvp in active.SkillEvents)
                {
                    if (!snapshot.SkillEvents.ContainsKey(kvp.Key))
                        snapshot.SkillEvents[kvp.Key] = kvp.Value;
                }

                // Carry forward per-combatant Skills/HealingSkills when the
                // incoming snapshot has less data (tracker restarted on reload).
                foreach (var ac in active.Combatants)
                {
                    var sc = snapshot.Combatants.Find(c =>
                        string.Equals(c.Name, ac.Name, StringComparison.OrdinalIgnoreCase));
                    if (sc == null) continue;

                    var scDmg = sc.Skills?.Sum(s => s.TotalDamage) ?? 0;
                    var acDmg = ac.Skills?.Sum(s => s.TotalDamage) ?? 0;
                    if (acDmg > scDmg && ac.Skills != null)
                        sc.Skills = ac.Skills;

                    var scHeal = sc.HealingSkills?.Sum(s => s.TotalDamage) ?? 0;
                    var acHeal = ac.HealingSkills?.Sum(s => s.TotalDamage) ?? 0;
                    if (acHeal > scHeal && ac.HealingSkills != null)
                        sc.HealingSkills = ac.HealingSkills;
                }

                snapshot.Timestamp = active.Timestamp;
            }

            // IINACT trims its Combatants list to participants that recently
            // dealt or took damage, so idle or distant alliance members blink
            // out mid-fight.
            if (active != null && prevSnapshotActive && active != snapshot)
            {
                foreach (var prev in active.Combatants)
                {
                    var stillPresent = snapshot.Combatants.Any(c =>
                        string.Equals(c.Name, prev.Name, StringComparison.OrdinalIgnoreCase));
                    if (!stillPresent)
                        snapshot.Combatants.Add(prev);
                }
            }

            active = snapshot;
            prevSnapshotActive = snapshot.Encounter.IsActive;

            return archived;
        }
    }

    public void RemoveHistory(int index)
    {
        lock (syncLock)
        {
            if (index >= 0 && index < history.Count)
            {
                var snap = history[index];
                history.RemoveAt(index);
                dirty = true;
                if (timelineStore != null && snap.HasTimeline)
                    timelineStore.Delete(snap.Id);
            }
        }
    }

    public void RemoveActive()
    {
        lock (syncLock)
        {
            if (sampleDataActive) return;
            active = null;
            prevSnapshotActive = false;
            isStaleDataSuppressed = true;
            dirty = true;
        }
    }

    public bool ArchiveActive()
    {
        lock (syncLock)
        {
            if (active == null || sampleDataActive)
                return false;

            active.Encounter.IsActive = false;
            AssignIdIfMissing(active);

            if (!double.IsNaN(active.Encounter.EncDps)
                && !(config.SkipZeroEdpsEncounters && active.Encounter.EncDps == 0))
            {
                if (!activeAlreadyInHistory)
                {
                    history.Add(active);
                    SaveTimelineSidecarLocked(active);
                }
                dirty = true;
            }

            activeAlreadyInHistory = false;
            active = null;
            prevSnapshotActive = false;
            return true;
        }
    }

    /// <summary>
    /// Copies the active encounter into history without removing it from
    /// the active slot, so the main window continues to display it.
    /// </summary>
    public bool CopyActiveToHistory()
    {
        lock (syncLock)
        {
            if (active == null || sampleDataActive || activeAlreadyInHistory)
                return false;

            active.Encounter.IsActive = false;
            AssignIdIfMissing(active);

            if (double.IsNaN(active.Encounter.EncDps))
                return false;

            if (config.SkipZeroEdpsEncounters && active.Encounter.EncDps == 0)
                return false;

            history.Add(active);
            SaveTimelineSidecarLocked(active);
            dirty = true;
            activeAlreadyInHistory = true;
            PruneHistoryLocked();
            return true;
        }
    }

    public bool RestoreLatestForPlayer(string playerName)
    {
        lock (syncLock)
        {
            var idx = -1;
            for (var i = history.Count - 1; i >= 0; i--)
            {
                if (string.Equals(history[i].PlayerName, playerName, StringComparison.OrdinalIgnoreCase))
                {
                    idx = i;
                    break;
                }
            }

            if (idx < 0 && history.Count > 0)
                idx = history.Count - 1;

            if (idx < 0)
                return false;

            active = history[idx];
            history.RemoveAt(idx);
            prevSnapshotActive = false;
            dirty = true;
            return true;
        }
    }

    public void Clear()
    {
        lock (syncLock)
        {
            if (timelineStore != null)
            {
                foreach (var snap in history)
                {
                    if (snap.HasTimeline)
                        timelineStore.Delete(snap.Id);
                }
            }
            history.Clear();
            active = null;
            prevSnapshotActive = false;
            dirty = true;
        }
    }

    public void SetSavePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Save path must not be null or empty.", nameof(path));
        savePath = path;
        timelineStore = new TimelineSidecarStore(path);
    }

    public void Load()
    {
        if (string.IsNullOrEmpty(savePath) || !System.IO.File.Exists(savePath))
        {
            loadedSuccessfully = true;
            return;
        }

        try
        {
            var json = System.IO.File.ReadAllText(savePath);
            var loaded = JsonConvert.DeserializeObject<List<EncounterSnapshot>>(json);
            if (loaded != null)
            {
                var anyRepaired = false;
                loaded.RemoveAll(s => double.IsNaN(s.Encounter.EncDps));
                foreach (var snapshot in loaded)
                {
                    // History entries are never live — clear stale active flags
                    // that may have been persisted by older versions.
                    if (snapshot.Encounter.IsActive)
                    {
                        snapshot.Encounter.IsActive = false;
                        anyRepaired = true;
                    }

                    if (snapshot.ValidateAndRepair())
                        anyRepaired = true;
                }

                lock (syncLock)
                {
                    history.Clear();
                    history.AddRange(loaded);

                    if (anyRepaired)
                        dirty = true;
                }
            }

            loadedSuccessfully = true;

            PruneHistory();

            // Persist repaired data back to disk so the rebuild is a one-time migration.
            Save();
        }
        catch (JsonException ex)
        {
            ServiceManager.LogWarning(LogChannel.EncounterStore, $"Encounter history is corrupt and could not be loaded: {ex.Message}");
        }
        catch (System.IO.IOException ex)
        {
            ServiceManager.LogWarning(LogChannel.EncounterStore, $"Failed to read encounter history file: {ex.Message}");
        }
        catch (Exception ex)
        {
            ServiceManager.LogWarning(LogChannel.EncounterStore, $"Unexpected error loading encounter history: {ex.Message}");
        }
    }

    /// <summary>Ensure a snapshot has a stable, unique Id. Idempotent.</summary>
    private static void AssignIdIfMissing(EncounterSnapshot snapshot)
    {
        if (snapshot.Id == 0)
            snapshot.Id = snapshot.Timestamp.ToUniversalTime().Ticks;
    }

    /// <summary>
    /// Load the snapshot's timeline sidecar into its in-memory dictionaries if
    /// not already loaded. Safe to call repeatedly. If the sidecar is missing or
    /// unreadable, <see cref="EncounterSnapshot.HasTimeline"/> is flipped to false
    /// and the dictionaries stay empty.
    /// </summary>
    public void EnsureTimelineLoaded(EncounterSnapshot snapshot)
    {
        if (timelineStore == null) return;
        if (!snapshot.HasTimeline) return;

        lock (syncLock)
        {
            if (snapshot.TimelineLoaded) return;
        }

        var bundle = timelineStore.Load(snapshot.Id);

        lock (syncLock)
        {
            if (snapshot.TimelineLoaded) return;

            if (bundle == null)
            {
                snapshot.HasTimeline = false;
                dirty = true;
                return;
            }

            bundle.CopyInto(snapshot);
            snapshot.TimelineLoaded = true;
        }
    }

    private void SaveTimelineSidecarLocked(EncounterSnapshot snapshot)
    {
        if (timelineStore == null) return;
        var bundle = TimelineBundle.FromSnapshot(snapshot);
        if (bundle.IsEmpty)
        {
            snapshot.HasTimeline = false;
            return;
        }
        if (timelineStore.Save(bundle))
        {
            snapshot.HasTimeline = true;
            snapshot.TimelineLoaded = true;
        }
    }

    public void PruneHistory()
    {
        lock (syncLock)
            PruneHistoryLocked();
    }

    public int CountZeroEdpsEncounters()
    {
        lock (syncLock)
            return history.Count(s => s.Encounter.EncDps == 0);
    }

    public int RemoveZeroEdpsEncounters()
    {
        lock (syncLock)
        {
            var before = history.Count;
            history.RemoveAll(s => s.Encounter.EncDps == 0);
            var removed = before - history.Count;
            if (removed > 0)
                dirty = true;
            return removed;
        }
    }

    private void PruneHistoryLocked()
    {
        var removed = false;

        if (config.HistoryLimitMode == HistoryLimitMode.Count)
        {
            while (history.Count > config.MaxEncounterHistory && config.MaxEncounterHistory > 0)
            {
                history.RemoveAt(0);
                removed = true;
            }
        }
        else if (config.HistoryLimitMode == HistoryLimitMode.Days)
        {
            var cutoff = DateTime.UtcNow.AddDays(-config.MaxEncounterHistoryDays);
            var before = history.Count;
            history.RemoveAll(s => s.Timestamp < cutoff);
            removed = history.Count < before;
        }

        if (removed)
            dirty = true;
    }

    private static readonly JsonSerializerSettings ExportSettings = new()
    {
        DefaultValueHandling = DefaultValueHandling.Ignore,
        Formatting = Formatting.Indented,
    };

    public string ExportEncounter(EncounterSnapshot encounter)
    {
        EnsureTimelineLoaded(encounter);
        var composite = new
        {
            Summary = encounter,
            Timeline = encounter.HasTimeline ? TimelineBundle.FromSnapshot(encounter) : null,
        };
        return JsonConvert.SerializeObject(composite, ExportSettings);
    }

    public EncounterSnapshot? ImportEncounter(string json, out string? error)
    {
        error = null;
        try
        {
            var snapshot = JsonConvert.DeserializeObject<EncounterSnapshot>(json);
            if (snapshot == null)
            {
                error = "Failed to parse encounter JSON.";
                return null;
            }

            if (double.IsNaN(snapshot.Encounter.EncDps))
            {
                error = "Invalid encounter data (NaN DPS).";
                return null;
            }

            snapshot.Encounter.IsActive = false;
            snapshot.ValidateAndRepair();

            lock (syncLock)
            {
                AssignIdIfMissing(snapshot);
                var idx = history.FindIndex(s => s.Timestamp > snapshot.Timestamp);
                if (idx < 0)
                    history.Add(snapshot);
                else
                    history.Insert(idx, snapshot);
                SaveTimelineSidecarLocked(snapshot);

                dirty = true;
                PruneHistoryLocked();
            }

            return snapshot;
        }
        catch (JsonException ex)
        {
            error = $"Invalid JSON: {ex.Message}";
            return null;
        }
    }

    public string GetExportsDirectory()
    {
        var dir = string.IsNullOrEmpty(savePath)
            ? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DamageTerror", "exports")
            : System.IO.Path.Combine(System.IO.Path.GetDirectoryName(savePath)!, "exports");
        System.IO.Directory.CreateDirectory(dir);
        return dir;
    }

    public void Save(bool force = false)
    {
        if (string.IsNullOrEmpty(savePath))
            return;

        List<EncounterSnapshot> snapshot;
        lock (syncLock)
        {
            if (!force && !dirty)
                return;

            // Don't overwrite the file with empty data when Load failed,
            // as that would permanently wipe previously saved history.
            if (!loadedSuccessfully && history.Count == 0)
                return;

            dirty = false;
            snapshot = new List<EncounterSnapshot>(history);
        }

        var path = savePath;
        Task.Run(() =>
        {
            try
            {
                var json = JsonConvert.SerializeObject(snapshot, Formatting.None, new JsonSerializerSettings
                {
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                });

                var dir = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    System.IO.Directory.CreateDirectory(dir);

                System.IO.File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                ServiceManager.LogWarning(LogChannel.EncounterStore, $"Failed to save encounter history: {ex.Message}");
            }
        });
    }
}
