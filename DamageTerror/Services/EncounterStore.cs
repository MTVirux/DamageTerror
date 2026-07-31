using System.Text;

namespace DamageTerror.Services;

public sealed class EncounterStore
{
    private readonly object syncLock = new();
    private readonly List<EncounterSnapshot> history = new();
    private readonly Dictionary<EncounterSnapshot, EncounterDiskSize?> diskSizeCache = new();
    private readonly Configuration config;
    private Task measureQueue = Task.CompletedTask;
    private int diskSizeVersion;
    private EncounterSnapshot? active;
    private bool prevSnapshotActive;
    /// <summary>When true, drop incoming CombatData until a genuinely new encounter starts.
    /// Set after the user manually removes the active encounter via RemoveActive().</summary>
    private bool isStaleDataSuppressed;
    private bool activeAlreadyInHistory;
    private string? savePath;
    private TimelineSidecarStore? timelineStore;
    private SidecarStore<RawCaptureBundle>? rawStore;
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
                var info = new FileInfo(savePath);
                return info.Exists ? info.Length : 0;
            }
            catch { return 0; }
        }
    }

    public long TimelineStorageSizeBytes => timelineStore?.TotalSizeBytes() ?? 0;
    public int TimelineFileCount => timelineStore?.FileCount() ?? 0;
    public string? TimelineDirectory => timelineStore?.DirectoryPath;

    public long RawCaptureStorageSizeBytes => rawStore?.TotalSizeBytes() ?? 0;
    public int RawCaptureFileCount => rawStore?.FileCount() ?? 0;

    /// <summary>
    /// On-disk footprint of a single encounter: its slice of encounters.json plus
    /// its timeline and raw capture sidecars. Sizing the summary means serializing it, which is far
    /// too slow for a draw call, so the work is queued onto a background worker:
    /// this returns false until the result lands. Results are cached until the
    /// store next changes.
    /// </summary>
    public bool TryGetDiskSize(EncounterSnapshot snapshot, out EncounterDiskSize size)
    {
        size = default;
        lock (syncLock)
        {
            if (diskSizeCache.TryGetValue(snapshot, out var cached))
            {
                // A null entry is queued, in flight, or failed to measure; either
                // way it is retried only after the next store change.
                if (cached == null)
                    return false;
                size = cached.Value;
                return true;
            }

            diskSizeCache[snapshot] = null;
            var version = diskSizeVersion;
            // Chained so a long history is measured one encounter at a time.
            measureQueue = measureQueue.ContinueWith(_ => MeasureDiskSize(snapshot, version));
        }

        return false;
    }

    private void MeasureDiskSize(EncounterSnapshot snapshot, int version)
    {
        long summaryBytes;
        try
        {
            using var counter = new Utf8ByteCounter();
            JsonSerializer.Create(SaveSettings).Serialize(counter, snapshot);
            summaryBytes = counter.ByteCount;
        }
        catch (Exception ex)
        {
            ServiceManager.LogWarning(LogChannel.EncounterStore,
                $"Failed to measure encounter size: {ex.Message}");
            return;
        }

        var timelineBytes = snapshot.HasTimeline ? timelineStore?.SizeBytes(snapshot.Id) ?? 0 : 0;
        var rawBytes = snapshot.HasRawCapture ? rawStore?.SizeBytes(snapshot.Id) ?? 0 : 0;

        lock (syncLock)
        {
            if (version == diskSizeVersion)
                diskSizeCache[snapshot] = new EncounterDiskSize(summaryBytes, timelineBytes, rawBytes);
        }
    }

    private void MarkDirtyLocked()
    {
        dirty = true;
        InvalidateDiskSizesLocked();
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
            MarkDirtyLocked();
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
                MarkDirtyLocked();
                if (timelineStore != null && snap.HasTimeline)
                    timelineStore.Delete(snap.Id);
                if (rawStore != null && snap.HasRawCapture)
                    rawStore.Delete(snap.Id);
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
            MarkDirtyLocked();
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
                MarkDirtyLocked();
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
            MarkDirtyLocked();
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
            MarkDirtyLocked();
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
                foreach (var snap in history)
                {
                    if (snap.HasTimeline)
                        timelineStore.Delete(snap.Id);
                }
            }
            history.Clear();
            active = null;
            prevSnapshotActive = false;
            MarkDirtyLocked();
        }
    }

    public void SetSavePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Save path must not be null or empty.", nameof(path));
        savePath = path;
        timelineStore = new TimelineSidecarStore(path);
        rawStore = new SidecarStore<RawCaptureBundle>(path, "raw");
    }

    public void Load()
    {
        if (string.IsNullOrEmpty(savePath) || !File.Exists(savePath))
        {
            loadedSuccessfully = true;
            return;
        }

        try
        {
            var json = File.ReadAllText(savePath);
            var loaded = JsonConvert.DeserializeObject<List<EncounterSnapshot>>(json);
            if (loaded != null)
            {
                var anyRepaired = false;
                lock (syncLock)
                {
                    if (MigrateEmbeddedTimelinesLocked(json, loaded))
                        anyRepaired = true;
                    if (MigrateEmbeddedRawCaptureLocked(loaded))
                        anyRepaired = true;
                }

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
                        MarkDirtyLocked();
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
        catch (IOException ex)
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
                MarkDirtyLocked();
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
                MarkDirtyLocked();
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
    /// </summary>
    private bool MigrateEmbeddedTimelinesLocked(string fileJson, List<EncounterSnapshot> loaded)
    {
        if (timelineStore == null) return false;
        JArray jarr;
        try
        {
            jarr = JArray.Parse(fileJson);
        }
        catch
        {
            return false;
        }
        if (jarr.Count != loaded.Count)
            return false;

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
                $"Timeline migration: {failed} encounter(s) failed to write sidecar. encounters.json will NOT be rewritten this load; will retry on next launch.");
            return false;
        }
        return migrated > 0;
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
            // Leaves the in-memory lists alone when the write fails, so the data
            // survives for the rest of the session and is retried next launch.
            SaveRawSidecarLocked(snap);

            if (snap.HasRawCapture)
                migrated++;
            else
                failed++;
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
        if (timelineStore.Save(bundle))
        {
            snapshot.HasTimeline = true;
            snapshot.TimelineLoaded = true;
        }
    }

    /// <summary>
    /// Write the snapshot's debug raw capture to its sidecar, then drop the in-memory
    /// copy. Unlike the timeline this does not stay resident: a single fight's raw
    /// capture runs to tens of megabytes, so it is re-read on demand instead.
    /// </summary>
    private void SaveRawSidecarLocked(EncounterSnapshot snapshot)
    {
        if (rawStore == null) return;
        var bundle = RawCaptureBundle.FromSnapshot(snapshot);
        if (bundle.IsEmpty)
            return;
        if (rawStore.Save(snapshot.Id, bundle))
        {
            snapshot.HasRawCapture = true;
            snapshot.RawLogLines = new List<string>();
            snapshot.RawCombatDataFrames = new List<string>();
            snapshot.RawCaptureLoaded = false;
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
                MarkDirtyLocked();
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
            MarkDirtyLocked();

        PruneTimelinesLocked();
        PruneRawCaptureLocked();
    }

    /// <summary>Delete raw capture sidecars no history entry claims. Raw capture has no
    /// retention setting of its own - it lives and dies with its encounter.</summary>
    private void PruneRawCaptureLocked()
    {
        if (rawStore == null) return;

        var referenced = new HashSet<long>();
        foreach (var snap in history)
        {
            if (snap.HasRawCapture)
                referenced.Add(snap.Id);
        }

        foreach (var id in rawStore.EnumerateIds().ToList())
        {
            if (!referenced.Contains(id))
                rawStore.Delete(id);
        }
    }

    private void PruneTimelinesLocked()
    {
        if (timelineStore == null) return;

        var referenced = new HashSet<long>();
        foreach (var snap in history)
        {
            if (snap.HasTimeline)
                referenced.Add(snap.Id);
        }

        foreach (var id in timelineStore.EnumerateIds().ToList())
        {
            if (!referenced.Contains(id))
                timelineStore.Delete(id);
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

        var purged = false;
        foreach (var snap in toPurge)
        {
            if (timelineStore.Delete(snap.Id))
            {
                snap.HasTimeline = false;
                snap.TimelineLoaded = false;
                snap.SkillEvents.Clear();
                snap.GraphData.Clear();
                snap.DamageTakenEvents.Clear();
                snap.ItemEvents.Clear();
                snap.StatusHistory.Clear();
                snap.StatusesReceived.Clear();
                purged = true;
            }
        }
        if (purged)
            MarkDirtyLocked();
    }

    private static readonly JsonSerializerSettings ExportSettings = new()
    {
        DefaultValueHandling = DefaultValueHandling.Ignore,
        Formatting = Formatting.Indented,
    };

    /// <summary>Settings encounters.json is written with. Per-encounter size
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

                MarkDirtyLocked();
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
            // Callers that mutate a snapshot in place (rather than the store) still
            // reach here, so drop measured sizes on every write.
            InvalidateDiskSizesLocked();
            snapshot = new List<EncounterSnapshot>(history);
        }

        var path = savePath;
        Task.Run(() =>
        {
            try
            {
                var json = JsonConvert.SerializeObject(snapshot, SaveSettings);

                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                ServiceManager.LogWarning(LogChannel.EncounterStore, $"Failed to save encounter history: {ex.Message}");
            }
        });
    }

    /// <summary>Measures the UTF-8 length of what is written to it and discards
    /// the text, so a snapshot can be sized without building its JSON string.</summary>
    private sealed class Utf8ByteCounter : TextWriter
    {
        private readonly Encoder encoder = Encoding.UTF8.GetEncoder();

        public long ByteCount { get; private set; }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            Span<char> one = [value];
            ByteCount += encoder.GetByteCount(one, false);
        }

        public override void Write(char[] buffer, int index, int count)
            => ByteCount += encoder.GetByteCount(buffer.AsSpan(index, count), false);

        public override void Write(string? value)
        {
            if (!string.IsNullOrEmpty(value))
                ByteCount += encoder.GetByteCount(value.AsSpan(), false);
        }
    }
}

/// <summary>Bytes a single encounter occupies on disk, split by file.</summary>
public readonly record struct EncounterDiskSize(long SummaryBytes, long TimelineBytes, long RawBytes)
{
    public long TotalBytes => SummaryBytes + TimelineBytes + RawBytes;
}
