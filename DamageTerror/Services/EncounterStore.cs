namespace DamageTerror.Services;

public sealed class EncounterStore
{
    private readonly object syncLock = new();
    private readonly List<EncounterSnapshot> history = new();
    /// <summary>Ids of history entries whose summary file is out of date.</summary>
    private readonly HashSet<long> dirtyIds = new();
    private readonly Dictionary<EncounterSnapshot, EncounterDiskSize> diskSizeCache = new();
    /// <summary>Bumped on every disk size invalidation so a measurement taken
    /// across one is discarded instead of caching a stale size.</summary>
    private int diskSizeVersion;
    private readonly Configuration config;
    /// <summary>FIFO queue for all sidecar and history file I/O so archive paths
    /// never block the data thread (or the UI, via syncLock) on a disk write.</summary>
    private Task ioQueue = Task.CompletedTask;
    private EncounterSnapshot? active;
    private bool prevSnapshotActive;
    /// <summary>When true, drop incoming CombatData until a genuinely new encounter starts.
    /// Set after the user manually removes the active encounter via RemoveActive().</summary>
    private bool isStaleDataSuppressed;
    private bool activeAlreadyInHistory;
    private string? savePath;
    private TimelineSidecarStore? timelineStore;
    private SidecarStore<RawCaptureBundle>? rawStore;
    private SidecarStore<EncounterSnapshot>? summaryStore;
    /// <summary>While true, orphan sweeps are skipped because history may not
    /// reflect every encounter on disk.</summary>
    private bool historyIncomplete;
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

    public long StorageSizeBytes => summaryStore?.TotalSizeBytes() ?? 0;

    public long TimelineStorageSizeBytes => timelineStore?.TotalSizeBytes() ?? 0;
    public int TimelineFileCount => timelineStore?.FileCount() ?? 0;
    public string? TimelineDirectory => timelineStore?.DirectoryPath;

    public long RawCaptureStorageSizeBytes => rawStore?.TotalSizeBytes() ?? 0;
    public int RawCaptureFileCount => rawStore?.FileCount() ?? 0;

    /// <summary>
    /// On-disk footprint of a single encounter: its summary file plus its
    /// timeline and raw capture sidecars. Three file stats, cached until the
    /// store next changes. A summary write still queued shows as 0 bytes until
    /// the next store change re-measures it.
    /// </summary>
    public bool TryGetDiskSize(EncounterSnapshot snapshot, out EncounterDiskSize size)
    {
        int version;
        lock (syncLock)
        {
            if (diskSizeCache.TryGetValue(snapshot, out size))
                return true;
            version = diskSizeVersion;
        }

        var summaryBytes = summaryStore?.SizeBytes(snapshot.Id) ?? 0;
        var timelineBytes = snapshot.HasTimeline ? timelineStore?.SizeBytes(snapshot.Id) ?? 0 : 0;
        var rawBytes = snapshot.HasRawCapture ? rawStore?.SizeBytes(snapshot.Id) ?? 0 : 0;
        size = new EncounterDiskSize(summaryBytes, timelineBytes, rawBytes);

        lock (syncLock)
        {
            // Stats taken across an invalidation describe the old files, so show
            // them once but do not cache them.
            if (version == diskSizeVersion)
                diskSizeCache[snapshot] = size;
        }

        return true;
    }

    private void MarkSnapshotDirtyLocked(EncounterSnapshot snapshot)
    {
        AssignIdIfMissing(snapshot);
        dirtyIds.Add(snapshot.Id);
        InvalidateDiskSizesLocked();
    }

    /// <summary>Mark a history snapshot for a summary rewrite after an in-place
    /// mutation from outside the store (e.g. debug recalculation).</summary>
    public void MarkDirty(EncounterSnapshot snapshot)
    {
        lock (syncLock) MarkSnapshotDirtyLocked(snapshot);
    }

    /// <summary>Append work to the background I/O queue. Must be called while
    /// holding syncLock. The work runs without syncLock held.</summary>
    private void EnqueueIoLocked(Action work)
    {
        ioQueue = ioQueue.ContinueWith(_ =>
        {
            try { work(); }
            catch (Exception ex)
            {
                ServiceManager.LogWarning(LogChannel.EncounterStore,
                    $"Background encounter I/O failed: {ex.Message}");
            }
        }, TaskScheduler.Default);
    }

    /// <summary>Block until all queued file writes have landed. Called on plugin
    /// dispose so pending archives are not lost on unload.</summary>
    public void FlushPendingWrites(TimeSpan timeout)
    {
        Task pending;
        lock (syncLock) pending = ioQueue;
        try { pending.Wait(timeout); }
        catch (AggregateException) { }
    }

    private void InvalidateDiskSizesLocked()
    {
        diskSizeCache.Clear();
        diskSizeVersion++;
    }

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

    /// <summary>Stop an in-progress encounter replay and restore the pre-replay
    /// view. No-op if no replay is active.</summary>
    public void StopActiveReplay()
    {
        lock (syncLock)
        {
            if (replaySimulator == null) return;
            replaySimulator.Stop();
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

            if (!TryYieldReplayToLive(newFightStarting))
                return false;

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

            var archived = false;

            if (snapshot.Encounter.IsActive && !prevSnapshotActive && active != null)
                archived = ArchivePreviousIfNewFight();
            else if (!snapshot.Encounter.IsActive && !prevSnapshotActive && active != null
                     && active != snapshot
                     && (active.GraphData.Count > 0 || active.SkillEvents.Count > 0))
                CarryForwardRestoredData(snapshot);

            if (active != null && prevSnapshotActive && active != snapshot)
                ReAddTrimmedCombatants(snapshot);

            active = snapshot;
            prevSnapshotActive = snapshot.Encounter.IsActive;

            return archived;
        }
    }

    // Replay yields to live combat when a fresh fight starts. Sample data stays
    // sticky (its lifecycle is explicitly user-controlled). Returns false when
    // the incoming snapshot should be dropped (sample/replay still owns active).
    private bool TryYieldReplayToLive(bool newFightStarting)
    {
        if (!sampleDataActive)
            return true;

        if (replaySimulator != null && newFightStarting)
        {
            replaySimulator.Stop();
            replaySimulator = null;
            pendingFactory = null;
            sampleDataActive = false;
            active = previewBackup;
            previewBackup = null;
            // Treat the previous active as no-longer-live so the archive
            // branch closes it out before the new fight begins.
            prevSnapshotActive = false;
            return true;
        }

        return false;
    }

    private bool ArchivePreviousIfNewFight()
    {
        var current = active!;
        var archived = false;
        current.Encounter.IsActive = false;
        if (!activeAlreadyInHistory && ShouldArchive(current))
        {
            AssignIdIfMissing(current);
            history.Add(current);
            SaveTimelineSidecarLocked(current);
            SaveRawSidecarLocked(current);
            MarkSnapshotDirtyLocked(current);
            archived = true;
            PruneHistoryLocked();
        }
        activeAlreadyInHistory = false;
        return archived;
    }

    // The active encounter was restored from history and has persisted
    // graph/skill data. Carry the data forward to the incoming snapshot
    // instead of archiving (which would create a duplicate on reload).
    private void CarryForwardRestoredData(EncounterSnapshot snapshot)
    {
        var current = active!;
        foreach (var kvp in current.GraphData)
        {
            if (!snapshot.GraphData.ContainsKey(kvp.Key))
                snapshot.GraphData[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in current.SkillEvents)
        {
            if (!snapshot.SkillEvents.ContainsKey(kvp.Key))
                snapshot.SkillEvents[kvp.Key] = kvp.Value;
        }

        // Carry forward per-combatant Skills/HealingSkills when the
        // incoming snapshot has less data (tracker restarted on reload).
        foreach (var ac in current.Combatants)
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

        snapshot.Timestamp = current.Timestamp;
    }

    // IINACT trims its Combatants list to participants that recently dealt or
    // took damage, so idle or distant alliance members blink out mid-fight.
    private void ReAddTrimmedCombatants(EncounterSnapshot snapshot)
    {
        foreach (var prev in active!.Combatants)
        {
            var stillPresent = snapshot.Combatants.Any(c =>
                string.Equals(c.Name, prev.Name, StringComparison.OrdinalIgnoreCase));
            if (!stillPresent)
                snapshot.Combatants.Add(prev);
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
                dirtyIds.Remove(snap.Id);
                InvalidateDiskSizesLocked();
                if (summaryStore != null && snap.Id != 0)
                {
                    var ss = summaryStore;
                    EnqueueIoLocked(() => ss.Delete(snap.Id));
                }
                if (timelineStore != null && snap.HasTimeline)
                {
                    var ts = timelineStore;
                    EnqueueIoLocked(() => ts.Delete(snap.Id));
                }
                if (rawStore != null && snap.HasRawCapture)
                {
                    var rs = rawStore;
                    EnqueueIoLocked(() => rs.Delete(snap.Id));
                }
            }
        }
    }

    public void RemoveActive()
    {
        lock (syncLock)
        {
            if (sampleDataActive) return;
            // A restored-from-history encounter still has its summary file on
            // disk; delete it or the removed encounter resurrects on next load.
            // Skip when it is (also) a live history entry (CopyActiveToHistory).
            if (active != null && active.Id != 0
                && !activeAlreadyInHistory && !history.Contains(active)
                && summaryStore != null)
            {
                var id = active.Id;
                dirtyIds.Remove(id);
                var ss = summaryStore;
                EnqueueIoLocked(() => ss.Delete(id));
            }
            active = null;
            activeAlreadyInHistory = false;
            prevSnapshotActive = false;
            isStaleDataSuppressed = true;
            InvalidateDiskSizesLocked();
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

            if (ShouldArchive(active))
            {
                if (!activeAlreadyInHistory)
                {
                    history.Add(active);
                    SaveTimelineSidecarLocked(active);
                    SaveRawSidecarLocked(active);
                }
                MarkSnapshotDirtyLocked(active);
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

            if (!ShouldArchive(active))
                return false;

            history.Add(active);
            SaveTimelineSidecarLocked(active);
            SaveRawSidecarLocked(active);
            MarkSnapshotDirtyLocked(active);
            activeAlreadyInHistory = true;
            PruneHistoryLocked();
            return true;
        }
    }

    public bool RestoreLatestForPlayer(string playerName)
    {
        EncounterSnapshot? toHydrate;
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
            toHydrate = active;
        }

        EnsureTimelineLoaded(toHydrate);
        return true;
    }

    public void Clear()
    {
        lock (syncLock)
        {
            if (timelineStore != null)
            {
                var ids = new List<long>();
                foreach (var snap in history)
                {
                    if (snap.HasTimeline)
                        ids.Add(snap.Id);
                }
                if (ids.Count > 0)
                {
                    var ts = timelineStore;
                    EnqueueIoLocked(() =>
                    {
                        foreach (var id in ids)
                            ts.Delete(id);
                    });
                }
            }
            if (summaryStore != null)
            {
                var ss = summaryStore;
                EnqueueIoLocked(() =>
                {
                    foreach (var id in ss.EnumerateIds().ToList())
                        ss.Delete(id);
                });
            }
            history.Clear();
            dirtyIds.Clear();
            active = null;
            prevSnapshotActive = false;
            InvalidateDiskSizesLocked();
        }
    }

    public void SetSavePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Save path must not be null or empty.", nameof(path));
        savePath = path;
        summaryStore = new SidecarStore<EncounterSnapshot>(path, "summaries", SaveSettings);
        timelineStore = new TimelineSidecarStore(path);
        rawStore = new SidecarStore<RawCaptureBundle>(path, "raw");
    }

    public void Load()
    {
        if (summaryStore == null)
            return;

        List<EncounterSnapshot>? monolithLeftover = null;
        var loaded = new List<EncounterSnapshot>();
        var repairedIds = new List<long>();
        var loadOk = true;

        // A read failure must not take plugin init down with it: log and carry on
        // with whatever was read, as the pre-split loader did.
        try
        {
            if (!string.IsNullOrEmpty(savePath) && File.Exists(savePath))
            {
                if (!TryMigrateMonolith(out monolithLeftover))
                {
                    loadOk = false;
                    ServiceManager.LogWarning(LogChannel.EncounterStore,
                        "encounters.json migration incomplete; the file is kept and will be retried on next launch.");
                }
            }

            foreach (var id in summaryStore.EnumerateIds().ToList())
            {
                var snap = summaryStore.Load(id);
                if (snap == null)
                {
                    // The id came from the directory listing, so null means the
                    // file is unreadable rather than absent - its sidecars must
                    // not be treated as orphans.
                    loadOk = false;
                    continue;
                }
                if (double.IsNaN(snap.Encounter.EncDps)) continue;
                if (snap.Id == 0) snap.Id = id;

                var repaired = false;
                // History entries are never live - clear stale active flags.
                if (snap.Encounter.IsActive)
                {
                    snap.Encounter.IsActive = false;
                    repaired = true;
                }
                if (snap.ValidateAndRepair())
                    repaired = true;
                if (repaired)
                    repairedIds.Add(snap.Id);

                loaded.Add(snap);
            }
        }
        catch (IOException ex)
        {
            loadOk = false;
            ServiceManager.LogWarning(LogChannel.EncounterStore,
                $"Failed to read encounter history: {ex.Message}");
        }
        catch (Exception ex)
        {
            loadOk = false;
            ServiceManager.LogWarning(LogChannel.EncounterStore,
                $"Unexpected error loading encounter history: {ex.Message}");
        }

        // Entries a failed migration could not write out still exist in memory
        // this session; the monolith stays on disk for a retry next launch.
        if (monolithLeftover != null)
        {
            var known = new HashSet<long>(loaded.ConvertAll(s => s.Id));
            foreach (var snap in monolithLeftover)
            {
                if (!known.Contains(snap.Id))
                    loaded.Add(snap);
            }
        }

        loaded.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

        lock (syncLock)
        {
            history.Clear();
            history.AddRange(loaded);
            foreach (var id in repairedIds)
                dirtyIds.Add(id);
            historyIncomplete = !loadOk;
        }

        // Pruning deletes every timeline and raw sidecar no history entry claims,
        // so it must not run on a history known to be incomplete - it would wipe
        // the sidecars of the encounters that failed to load.
        if (loadOk)
        {
            PruneHistory();
            Save();
        }
    }

    /// <summary>
    /// One-time migration of the pre-split encounters.json monolith into
    /// per-encounter summary files (plus timeline/raw sidecars for the very old
    /// embedded format). Returns true and deletes the monolith only when every
    /// summary and every embedded timeline was written; on failure returns false
    /// with the parsed entries in <paramref name="leftover"/> so this session
    /// still sees its history and the monolith survives for a retry.
    /// </summary>
    private bool TryMigrateMonolith(out List<EncounterSnapshot>? leftover)
    {
        leftover = null;
        string json;
        try
        {
            json = File.ReadAllText(savePath!);
        }
        catch (IOException ex)
        {
            ServiceManager.LogWarning(LogChannel.EncounterStore,
                $"Failed to read encounter history file: {ex.Message}");
            return false;
        }

        List<EncounterSnapshot>? parsed;
        try
        {
            parsed = JsonConvert.DeserializeObject<List<EncounterSnapshot>>(json);
        }
        catch (JsonException ex)
        {
            ServiceManager.LogWarning(LogChannel.EncounterStore,
                $"Encounter history is corrupt and could not be loaded: {ex.Message}");
            return false;
        }

        if (parsed == null || parsed.Count == 0)
        {
            TryDeleteMonolith();
            return true;
        }

        bool timelinesOk;
        lock (syncLock)
        {
            timelinesOk = MigrateEmbeddedTimelinesLocked(json, parsed);
            // Raw capture is debug-only and its own warning already says the data
            // is dropped, so a failed raw sidecar write does not hold the monolith
            // back. Embedded timelines have no other copy, so they do.
            MigrateEmbeddedRawCaptureLocked(parsed);
        }

        parsed.RemoveAll(s => double.IsNaN(s.Encounter.EncDps));

        var ok = timelinesOk;
        foreach (var snap in parsed)
        {
            snap.Encounter.IsActive = false;
            snap.ValidateAndRepair();
            AssignIdIfMissing(snap);
            if (!summaryStore!.Save(snap.Id, snap))
                ok = false;
        }

        if (!ok)
        {
            leftover = parsed;
            return false;
        }

        TryDeleteMonolith();
        ServiceManager.LogWarning(LogChannel.EncounterStore,
            $"Migrated {parsed.Count} encounter(s) to per-encounter summary files.");
        return true;
    }

    private void TryDeleteMonolith()
    {
        try
        {
            File.Delete(savePath!);
        }
        catch (IOException ex)
        {
            ServiceManager.LogWarning(LogChannel.EncounterStore,
                $"Could not remove migrated encounters.json: {ex.Message}");
        }
    }

    /// <summary>Ensure a snapshot has a stable, unique Id. Idempotent.</summary>
    private static void AssignIdIfMissing(EncounterSnapshot snapshot)
    {
        if (snapshot.Id == 0)
            snapshot.Id = snapshot.Timestamp.ToUniversalTime().Ticks;
    }

    /// <summary>Whether an encounter is eligible to be written to history.</summary>
    private bool ShouldArchive(EncounterSnapshot s)
        => !double.IsNaN(s.Encounter.EncDps)
           && !(config.SkipZeroEdpsEncounters && s.Encounter.EncDps == 0);

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
                MarkSnapshotDirtyLocked(snapshot);
                return;
            }

            bundle.CopyInto(snapshot);
            snapshot.TimelineLoaded = true;
        }
    }

    /// <summary>
    /// Load the snapshot's raw capture sidecar into its in-memory lists if not already
    /// loaded. Safe to call repeatedly. If the sidecar is missing or unreadable,
    /// <see cref="EncounterSnapshot.HasRawCapture"/> is flipped to false and the lists
    /// stay empty.
    /// </summary>
    public void EnsureRawCaptureLoaded(EncounterSnapshot snapshot)
    {
        if (rawStore == null) return;
        if (!snapshot.HasRawCapture) return;

        lock (syncLock)
        {
            if (snapshot.RawCaptureLoaded) return;
        }

        var bundle = rawStore.Load(snapshot.Id);

        lock (syncLock)
        {
            if (snapshot.RawCaptureLoaded) return;

            if (bundle == null)
            {
                snapshot.HasRawCapture = false;
                MarkSnapshotDirtyLocked(snapshot);
                return;
            }

            bundle.CopyInto(snapshot);
            snapshot.RawCaptureLoaded = true;
        }
    }

    /// <summary>
    /// Detect encounters loaded from the pre-split monolithic format and migrate
    /// their embedded timeline streams into sidecar files. The timeline dicts on
    /// <see cref="EncounterSnapshot"/> are [JsonIgnore], so the embedded data is
    /// pulled directly from the original JSON tokens before they are dropped.
    /// Returns false only when a sidecar write failed, meaning the caller must
    /// keep the monolith - it holds the only copy of that timeline data.
    /// Nothing to migrate counts as success.
    /// </summary>
    private bool MigrateEmbeddedTimelinesLocked(string fileJson, List<EncounterSnapshot> loaded)
    {
        if (timelineStore == null) return true;
        // Post-migration files never contain these keys (the properties are
        // [JsonIgnore] on EncounterSnapshot), so skip the expensive second
        // parse unless embedded timeline data is actually present.
        if (!fileJson.Contains("\"GraphData\"", StringComparison.Ordinal)
            && !fileJson.Contains("\"SkillEvents\"", StringComparison.Ordinal)
            && !fileJson.Contains("\"DamageTakenEvents\"", StringComparison.Ordinal)
            && !fileJson.Contains("\"ItemEvents\"", StringComparison.Ordinal)
            && !fileJson.Contains("\"StatusHistory\"", StringComparison.Ordinal)
            && !fileJson.Contains("\"StatusesReceived\"", StringComparison.Ordinal))
            return true;
        JArray jarr;
        try
        {
            jarr = JArray.Parse(fileJson);
        }
        catch
        {
            // Unrecoverable rather than transient: retrying next launch would fail
            // the same way and strand the monolith forever.
            return true;
        }
        if (jarr.Count != loaded.Count)
            return true;

        var migrated = 0;
        var failed = 0;
        for (int i = 0; i < jarr.Count; i++)
        {
            if (jarr[i] is not JObject jobj) continue;
            var snap = loaded[i];

            var bundle = new TimelineBundle { EncounterId = 0 };
            if (!PopulateBundleFromJson(jobj, bundle)) continue;

            AssignIdIfMissing(snap);
            bundle.EncounterId = snap.Id;

            if (timelineStore.Save(bundle))
            {
                snap.HasTimeline = true;
                snap.TimelineLoaded = false;
                migrated++;
            }
            else
            {
                failed++;
            }
        }
        if (migrated > 0)
            ServiceManager.LogWarning(LogChannel.EncounterStore,
                $"Migrated {migrated} encounter(s) to split-storage timeline sidecars.");
        if (failed > 0)
        {
            ServiceManager.LogWarning(LogChannel.EncounterStore,
                $"Timeline migration: {failed} encounter(s) failed to write sidecar. encounters.json is kept and migration will retry on next launch.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Move raw capture embedded by the pre-split format into sidecar files. Unlike the
    /// timeline streams these properties still deserialize, so the data is already on the
    /// loaded snapshots and no second parse of the file is needed - only the write-out
    /// and the in-memory clear.
    /// </summary>
    private bool MigrateEmbeddedRawCaptureLocked(List<EncounterSnapshot> loaded)
    {
        if (rawStore == null) return false;

        var migrated = 0;
        var failed = 0;
        foreach (var snap in loaded)
        {
            if (snap.RawLogLines.Count == 0 && snap.RawCombatDataFrames.Count == 0)
                continue;

            AssignIdIfMissing(snap);
            // Migration runs once during Load, before the UI exists - write inline
            // so the migrated/failed counts are accurate. Leaves the in-memory
            // lists alone when the write fails, so the data survives for the rest
            // of the session and is retried next launch.
            var bundle = RawCaptureBundle.FromSnapshot(snap);
            if (rawStore.Save(snap.Id, bundle))
            {
                snap.HasRawCapture = true;
                snap.RawLogLines = new List<string>();
                snap.RawCombatDataFrames = new List<string>();
                snap.RawCaptureLoaded = false;
                migrated++;
            }
            else
            {
                failed++;
            }
        }
        if (migrated > 0)
            ServiceManager.LogWarning(LogChannel.EncounterStore,
                $"Migrated {migrated} encounter(s) to raw capture sidecars.");
        if (failed > 0)
        {
            ServiceManager.LogWarning(LogChannel.EncounterStore,
                $"Raw capture migration: {failed} encounter(s) failed to write sidecar. Their raw capture is not on disk and will be dropped from encounters.json on the next save.");
            return false;
        }
        return migrated > 0;
    }

    private static bool TryPopulateDict<TValue>(
        JToken? token,
        Dictionary<string, List<TValue>> target)
    {
        if (token is not JObject jobj) return false;
        if (jobj.Count == 0) return false;
        var converted = jobj.ToObject<Dictionary<string, List<TValue>>>(JsonSerializer.CreateDefault());
        if (converted == null || converted.Count == 0) return false;
        foreach (var kvp in converted)
            target[kvp.Key] = kvp.Value;
        return true;
    }

    private static bool PopulateBundleFromJson(JObject jobj, TimelineBundle bundle)
    {
        var any = false;
        any |= TryPopulateDict(jobj["GraphData"], bundle.GraphData);
        any |= TryPopulateDict(jobj["SkillEvents"], bundle.SkillEvents);
        any |= TryPopulateDict(jobj["DamageTakenEvents"], bundle.DamageTakenEvents);
        any |= TryPopulateDict(jobj["ItemEvents"], bundle.ItemEvents);
        any |= TryPopulateDict(jobj["StatusHistory"], bundle.StatusHistory);
        any |= TryPopulateDict(jobj["StatusesReceived"], bundle.StatusesReceived);
        return any;
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

        // Optimistic: the timeline stays resident on the snapshot, so readers are
        // correct while the write is queued. Flipped back on failure.
        snapshot.HasTimeline = true;
        snapshot.TimelineLoaded = true;
        var store = timelineStore;
        EnqueueIoLocked(() =>
        {
            if (store.Save(bundle)) return;
            lock (syncLock)
            {
                snapshot.HasTimeline = false;
                snapshot.TimelineLoaded = false;
            }
        });
    }

    /// <summary>
    /// Queue the snapshot's debug raw capture write to its sidecar; the in-memory
    /// copy is dropped once the write lands. Unlike the timeline this does not stay
    /// resident: a single fight's raw capture runs to tens of megabytes, so it is
    /// re-read on demand instead.
    /// </summary>
    private void SaveRawSidecarLocked(EncounterSnapshot snapshot)
    {
        if (rawStore == null) return;
        var bundle = RawCaptureBundle.FromSnapshot(snapshot);
        if (bundle.IsEmpty)
            return;

        // Optimistic: lists stay resident until the write lands, so
        // EnsureRawCaptureLoaded never hits disk for a pending write.
        snapshot.HasRawCapture = true;
        snapshot.RawCaptureLoaded = true;
        var store = rawStore;
        EnqueueIoLocked(() =>
        {
            if (store.Save(snapshot.Id, bundle))
            {
                lock (syncLock)
                {
                    snapshot.RawLogLines = new List<string>();
                    snapshot.RawCombatDataFrames = new List<string>();
                    snapshot.RawCaptureLoaded = false;
                }
            }
            else
            {
                lock (syncLock) snapshot.HasRawCapture = false;
            }
        });
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
            var toRemove = history.FindAll(s => s.Encounter.EncDps == 0);
            if (toRemove.Count == 0)
                return 0;
            history.RemoveAll(s => s.Encounter.EncDps == 0);
            DeleteSummariesLocked(toRemove);
            InvalidateDiskSizesLocked();
            return toRemove.Count;
        }
    }

    private void PruneHistoryLocked()
    {
        var pruned = new List<EncounterSnapshot>();

        if (config.HistoryLimitMode == HistoryLimitMode.Count)
        {
            while (history.Count > config.MaxEncounterHistory && config.MaxEncounterHistory > 0)
            {
                pruned.Add(history[0]);
                history.RemoveAt(0);
            }
        }
        else if (config.HistoryLimitMode == HistoryLimitMode.Days)
        {
            var cutoff = DateTime.UtcNow.AddDays(-config.MaxEncounterHistoryDays);
            pruned.AddRange(history.FindAll(s => s.Timestamp < cutoff));
            history.RemoveAll(s => s.Timestamp < cutoff);
        }

        if (pruned.Count > 0)
        {
            DeleteSummariesLocked(pruned);
            InvalidateDiskSizesLocked();
        }

        PruneTimelinesLocked();
        PruneRawCaptureLocked();
    }

    /// <summary>Queue deletion of the summary files for snapshots that have been
    /// removed from history. Must be called while holding syncLock.</summary>
    private void DeleteSummariesLocked(List<EncounterSnapshot> snapshots)
    {
        if (summaryStore == null) return;
        var ids = new List<long>();
        foreach (var snap in snapshots)
        {
            if (snap.Id == 0) continue;
            dirtyIds.Remove(snap.Id);
            ids.Add(snap.Id);
        }
        if (ids.Count == 0) return;
        var ss = summaryStore;
        EnqueueIoLocked(() =>
        {
            foreach (var id in ids)
                ss.Delete(id);
        });
    }

    /// <summary>Delete raw capture sidecars no history entry claims. Raw capture has no
    /// retention setting of its own - it lives and dies with its encounter.</summary>
    private void PruneRawCaptureLocked()
    {
        if (rawStore == null) return;
        if (historyIncomplete) return;

        var referenced = new HashSet<long>();
        foreach (var snap in history)
        {
            if (snap.HasRawCapture)
                referenced.Add(snap.Id);
        }

        var rs = rawStore;
        EnqueueIoLocked(() =>
        {
            foreach (var id in rs.EnumerateIds().ToList())
            {
                if (!referenced.Contains(id))
                    rs.Delete(id);
            }
        });
    }

    private void PruneTimelinesLocked()
    {
        if (timelineStore == null) return;
        var ts = timelineStore;

        // The retention purge below stays active either way - it only touches
        // snapshots that are actually in history.
        if (!historyIncomplete)
        {
            var referenced = new HashSet<long>();
            foreach (var snap in history)
            {
                if (snap.HasTimeline)
                    referenced.Add(snap.Id);
            }

            EnqueueIoLocked(() =>
            {
                foreach (var id in ts.EnumerateIds().ToList())
                {
                    if (!referenced.Contains(id))
                        ts.Delete(id);
                }
            });
        }

        var withTimelines = history
            .Where(s => s.HasTimeline)
            .OrderByDescending(s => s.Timestamp)
            .ToList();

        IEnumerable<EncounterSnapshot> toPurge;
        if (config.TimelineRetentionMode == HistoryLimitMode.Count)
        {
            if (config.MaxTimelineCount <= 0) return;
            toPurge = withTimelines.Skip(config.MaxTimelineCount);
        }
        else
        {
            var cutoff = DateTime.UtcNow.AddDays(-config.MaxTimelineDays);
            toPurge = withTimelines.Where(s => s.Timestamp < cutoff);
        }

        foreach (var snap in toPurge)
        {
            snap.HasTimeline = false;
            snap.TimelineLoaded = false;
            MarkSnapshotDirtyLocked(snap);
            var purgeSnap = snap;
            // Delete and clear on the queue so a pending write of this same
            // encounter (FIFO-ordered ahead of us) finishes serializing first.
            EnqueueIoLocked(() =>
            {
                ts.Delete(purgeSnap.Id);
                lock (syncLock)
                {
                    purgeSnap.SkillEvents.Clear();
                    purgeSnap.GraphData.Clear();
                    purgeSnap.DamageTakenEvents.Clear();
                    purgeSnap.ItemEvents.Clear();
                    purgeSnap.StatusHistory.Clear();
                    purgeSnap.StatusesReceived.Clear();
                }
            });
        }
    }

    private static readonly JsonSerializerSettings ExportSettings = new()
    {
        DefaultValueHandling = DefaultValueHandling.Ignore,
        Formatting = Formatting.Indented,
    };

    /// <summary>Settings summary files are written with. Per-encounter size
    /// measurement reuses them so the reported bytes match what lands on disk.</summary>
    private static readonly JsonSerializerSettings SaveSettings = new()
    {
        DefaultValueHandling = DefaultValueHandling.Ignore,
        Formatting = Formatting.None,
    };

    public string ExportEncounter(EncounterSnapshot encounter)
    {
        EnsureTimelineLoaded(encounter);
        var composite = new
        {
            Summary = encounter,
            Timeline = encounter.HasTimeline ? TimelineBundle.FromSnapshot(encounter) : null,
            // Read straight from the sidecar rather than hydrating the snapshot, so a
            // single export does not leave tens of megabytes resident.
            RawCapture = encounter.HasRawCapture ? rawStore?.Load(encounter.Id) : null,
        };
        return JsonConvert.SerializeObject(composite, ExportSettings);
    }

    public EncounterSnapshot? ImportEncounter(string json, out string? error)
    {
        error = null;
        try
        {
            EncounterSnapshot? snapshot;
            var jobj = JObject.Parse(json);
            var summaryToken = jobj["Summary"];
            if (summaryToken != null)
            {
                snapshot = summaryToken.ToObject<EncounterSnapshot>();
                if (snapshot != null)
                {
                    var timelineToken = jobj["Timeline"];
                    if (timelineToken != null && timelineToken.Type != JTokenType.Null)
                    {
                        var bundle = timelineToken.ToObject<TimelineBundle>();
                        if (bundle != null)
                            bundle.CopyInto(snapshot);
                    }

                    var rawToken = jobj["RawCapture"];
                    if (rawToken != null && rawToken.Type != JTokenType.Null)
                        rawToken.ToObject<RawCaptureBundle>()?.CopyInto(snapshot);
                }
            }
            else
            {
                snapshot = jobj.ToObject<EncounterSnapshot>();
                if (snapshot != null)
                {
                    var legacyBundle = new TimelineBundle();
                    if (PopulateBundleFromJson(jobj, legacyBundle))
                        legacyBundle.CopyInto(snapshot);
                }
            }

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
                SaveRawSidecarLocked(snapshot);

                MarkSnapshotDirtyLocked(snapshot);
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
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DamageTerror", "exports")
            : Path.Combine(Path.GetDirectoryName(savePath)!, "exports");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public void Save(bool force = false)
    {
        lock (syncLock)
        {
            if (summaryStore == null)
                return;

            // After a failed legacy-file load, only new archives are known to be
            // intact; per-file writes cannot clobber the old monolith, so writing
            // dirty entries is always safe.
            List<EncounterSnapshot> toWrite;
            if (force)
            {
                toWrite = new List<EncounterSnapshot>(history);
            }
            else
            {
                if (dirtyIds.Count == 0)
                    return;
                toWrite = history.FindAll(s => dirtyIds.Contains(s.Id));
            }

            dirtyIds.Clear();
            InvalidateDiskSizesLocked();

            var ss = summaryStore;
            foreach (var snap in toWrite)
            {
                var s = snap;
                EnqueueIoLocked(() => ss.Save(s.Id, s));
            }
        }
    }
}

/// <summary>Bytes a single encounter occupies on disk, split by file.</summary>
public readonly record struct EncounterDiskSize(long SummaryBytes, long TimelineBytes, long RawBytes)
{
    public long TotalBytes => SummaryBytes + TimelineBytes + RawBytes;
}
