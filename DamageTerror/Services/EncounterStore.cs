using Dalamud.Plugin.Services;
using Newtonsoft.Json;

namespace DamageTerror.Services;

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

            if (snapshot.Encounter.IsActive && !wasActive && active != null)
            {
                history.Add(active);
                dirty = true;
                archived = true;

                while (history.Count > maxHistory)
                    history.RemoveAt(0);
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

    public bool ArchiveActive()
    {
        lock (syncLock)
        {
            if (active == null)
                return false;

            history.Add(active);
            dirty = true;

            while (history.Count > maxHistory)
                history.RemoveAt(0);

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
