using Dalamud.Plugin.Services;
using Newtonsoft.Json;

namespace DamageTerror.Services;

/// <summary>
/// Stores the active encounter and a history of past encounters.
/// Thread-safe: data arrives from background/IPC threads, UI reads on main thread.
/// </summary>
public class EncounterStore
{
    private readonly object syncLock = new();
    private readonly int maxHistory;
    private readonly List<EncounterSnapshot> history = new();
    private EncounterSnapshot? active;
    private bool wasActive;
    private string? savePath;
    private bool dirty;

    public EncounterStore(int maxHistory)
    {
        this.maxHistory = maxHistory;
    }

    /// <summary>
    /// The currently active encounter, or null if no data yet.
    /// </summary>
    public EncounterSnapshot? ActiveEncounter
    {
        get { lock (syncLock) return active; }
    }

    /// <summary>
    /// Past encounters in chronological order (oldest first).
    /// </summary>
    public List<EncounterSnapshot> History
    {
        get
        {
            lock (syncLock)
                return new List<EncounterSnapshot>(history);
        }
    }

    /// <summary>
    /// Total number of encounters available (history + active if present).
    /// </summary>
    public int TotalCount
    {
        get
        {
            lock (syncLock)
                return history.Count + (active != null ? 1 : 0);
        }
    }

    /// <summary>
    /// Gets an encounter by index (0 = oldest history, last = active).
    /// </summary>
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

    /// <summary>
    /// Update the store with a new CombatData snapshot.
    /// Handles encounter boundary detection (active → inactive transitions).
    /// </summary>
    public void Update(EncounterSnapshot snapshot)
    {
        lock (syncLock)
        {
            if (snapshot.Encounter.IsActive && !wasActive && active != null)
            {
                // New encounter started — archive the previous encounter
                history.Add(active);
                dirty = true;

                // Trim history if needed
                while (history.Count > maxHistory)
                    history.RemoveAt(0);
            }

            active = snapshot;
            wasActive = snapshot.Encounter.IsActive;
        }
    }

    /// <summary>
    /// Remove a history encounter by index.
    /// </summary>
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

    /// <summary>
    /// Clear all stored data.
    /// </summary>
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

    /// <summary>
    /// Set the file path used for persisting encounter history.
    /// </summary>
    public void SetSavePath(string path)
    {
        savePath = path;
    }

    /// <summary>
    /// Load encounter history from disk. Should be called once at startup.
    /// </summary>
    public void Load()
    {
        if (string.IsNullOrEmpty(savePath) || !System.IO.File.Exists(savePath))
            return;

        try
        {
            var json = System.IO.File.ReadAllText(savePath);
            var loaded = JsonConvert.DeserializeObject<List<EncounterSnapshot>>(json);
            if (loaded != null)
            {
                lock (syncLock)
                {
                    history.Clear();
                    history.AddRange(loaded);

                    // Trim to limit
                    while (history.Count > maxHistory)
                        history.RemoveAt(0);
                }
            }
        }
        catch
        {
            // If the file is corrupt, just start fresh
        }
    }

    /// <summary>
    /// Save encounter history to disk. Only writes if data has changed.
    /// </summary>
    public void Save(bool force = false)
    {
        if (string.IsNullOrEmpty(savePath))
            return;

        lock (syncLock)
        {
            if (!force && !dirty)
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
        catch
        {
            // Best-effort save
        }
    }
}
