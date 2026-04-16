using Dalamud.Plugin.Services;
using Newtonsoft.Json;

namespace DamageTerror.Services;

public class EncounterStore
{
    private readonly object syncLock = new();
    private readonly List<EncounterSnapshot> history = new();
    private readonly Configuration config;
    private EncounterSnapshot? active;
    private bool wasActive;
    /// <summary>When true, drop incoming CombatData until a genuinely new encounter starts.
    /// Set after the user manually removes the active encounter via RemoveActive().</summary>
    private bool isStaleDataSuppressed;
    private bool activeAlreadyInHistory;
    private string? savePath;
    private bool dirty;
    private bool loadedSuccessfully;
    private bool sampleDataActive;
    private EncounterSnapshot? previewBackup;
    private SampleCombatSimulator? sampleSimulator;
    private Func<CombatantEntry?>? pendingFactory;

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
        lock (syncLock)
        {
            if (index < 0) return null;
            if (index < history.Count) return history[index];
            if (index == history.Count && active != null) return active;
            return null;
        }
    }

    public bool IsSampleDataActive
    {
        get { lock (syncLock) return sampleDataActive; }
    }

    public bool IsSampleSimulating
    {
        get { lock (syncLock) return sampleSimulator?.IsRunning ?? false; }
    }

    public event Action? OnSampleDataLoaded;

    public void LoadSampleData(EncounterSnapshot sample, bool simulate = false, Func<CombatantEntry?>? combatantFactory = null)
    {
        lock (syncLock)
        {
            sampleSimulator?.Stop();
            sampleSimulator = null;

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
        }
    }

    public void ClearSampleData()
    {
        lock (syncLock)
        {
            if (!sampleDataActive) return;
            sampleSimulator?.Stop();
            sampleSimulator = null;
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
            if (sampleDataActive)
                return false;

            var archived = false;

            if (isStaleDataSuppressed)
            {
                if (snapshot.Encounter.IsActive && !wasActive)
                    isStaleDataSuppressed = false;
                else
                {
                    wasActive = snapshot.Encounter.IsActive;
                    return false;
                }
            }

            if (snapshot.Encounter.IsActive && !wasActive && active != null)
            {
                active.Encounter.IsActive = false;
                if (!activeAlreadyInHistory && !double.IsNaN(active.Encounter.EncDps)
                    && !(config.SkipZeroEdpsEncounters && active.Encounter.EncDps == 0))
                {
                    history.Add(active);
                    dirty = true;
                    archived = true;
                    PruneHistoryLocked();
                }
                activeAlreadyInHistory = false;
            }
            else if (!snapshot.Encounter.IsActive && !wasActive && active != null
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

            active = snapshot;
            wasActive = snapshot.Encounter.IsActive;

            return archived;
        }
    }

    public void RemoveHistory(int index)
    {
        lock (syncLock)
        {
            if (index >= 0 && index < history.Count)
            {
                history.RemoveAt(index);
                dirty = true;
            }
        }
    }

    public void RemoveActive()
    {
        lock (syncLock)
        {
            if (sampleDataActive) return;
            active = null;
            wasActive = false;
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

            if (!double.IsNaN(active.Encounter.EncDps)
                && !(config.SkipZeroEdpsEncounters && active.Encounter.EncDps == 0))
            {
                if (!activeAlreadyInHistory)
                    history.Add(active);
                dirty = true;
            }

            activeAlreadyInHistory = false;
            active = null;
            wasActive = false;
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

            if (double.IsNaN(active.Encounter.EncDps))
                return false;

            if (config.SkipZeroEdpsEncounters && active.Encounter.EncDps == 0)
                return false;

            history.Add(active);
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
            wasActive = false;
            dirty = true;
            return true;
        }
    }

    public void Clear()
    {
        lock (syncLock)
        {
            history.Clear();
            active = null;
            wasActive = false;
            dirty = true;
        }
    }

    public void SetSavePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Save path must not be null or empty.", nameof(path));
        savePath = path;
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
            ServiceManager.PluginLog.Warning($"Encounter history is corrupt and could not be loaded: {ex.Message}");
        }
        catch (System.IO.IOException ex)
        {
            ServiceManager.PluginLog.Warning($"Failed to read encounter history file: {ex.Message}");
        }
        catch (Exception ex)
        {
            ServiceManager.PluginLog.Warning($"Unexpected error loading encounter history: {ex.Message}");
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
        return JsonConvert.SerializeObject(encounter, ExportSettings);
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
                var idx = history.FindIndex(s => s.Timestamp > snapshot.Timestamp);
                if (idx < 0)
                    history.Add(snapshot);
                else
                    history.Insert(idx, snapshot);

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

        lock (syncLock)
        {
            if (!force && !dirty)
                return;

            // Don't overwrite the file with empty data when Load failed,
            // as that would permanently wipe previously saved history.
            if (!loadedSuccessfully && history.Count == 0)
                return;

            dirty = false;
        }

        try
        {
            List<EncounterSnapshot> snapshot;
            lock (syncLock)
            {
                snapshot = new List<EncounterSnapshot>(history);
            }

            var json = JsonConvert.SerializeObject(snapshot, Formatting.None, new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
            });

            var dir = System.IO.Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(dir))
                System.IO.Directory.CreateDirectory(dir);

            System.IO.File.WriteAllText(savePath, json);
        }
        catch (Exception ex)
        {
            ServiceManager.PluginLog.Warning($"Failed to save encounter history: {ex.Message}");
        }
    }
}
