using Dalamud.Plugin.Services;
using Newtonsoft.Json;

namespace DamageTerror.Services;

public class EncounterStore
{
    private readonly object syncLock = new();
    private readonly List<EncounterSnapshot> history = new();
    private EncounterSnapshot? active;
    private bool wasActive;
    private bool suppressActive;
    private string? savePath;
    private bool dirty;
    private bool loadedSuccessfully;

    public EncounterStore()
    {
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

    public bool Update(EncounterSnapshot snapshot)
    {
        lock (syncLock)
        {
            var archived = false;

            if (suppressActive)
            {
                if (snapshot.Encounter.IsActive && !wasActive)
                    suppressActive = false;
                else
                {
                    wasActive = snapshot.Encounter.IsActive;
                    return false;
                }
            }

            if (snapshot.Encounter.IsActive && !wasActive && active != null)
            {
                active.Encounter.IsActive = false;
                if (!double.IsNaN(active.Encounter.EncDps))
                {
                    history.Add(active);
                    dirty = true;
                    archived = true;
                    PruneHistoryLocked();
                }
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
            active = null;
            wasActive = false;
            suppressActive = true;
            dirty = true;
        }
    }

    public bool ArchiveActive()
    {
        lock (syncLock)
        {
            if (active == null)
                return false;

            active.Encounter.IsActive = false;

            if (!double.IsNaN(active.Encounter.EncDps))
            {
                history.Add(active);
                dirty = true;
            }

            active = null;
            wasActive = false;
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

            // Fall back to the latest entry if no match for this player.
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
        catch
        {
            // If the file is corrupt, just start fresh.
            // loadedSuccessfully stays false so Save won't overwrite the
            // existing file with an empty list.
        }
    }

    public void PruneHistory()
    {
        lock (syncLock)
            PruneHistoryLocked();
    }

    private void PruneHistoryLocked()
    {
        var config = DamageTerrorPlugin.Instance?.Config;
        if (config == null)
            return;

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
