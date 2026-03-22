using Dalamud.Plugin.Services;

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
            if (!snapshot.Encounter.IsActive && wasActive && active != null)
            {
                // Encounter ended — archive the previous active encounter
                history.Add(active);

                // Trim history if needed
                while (history.Count > maxHistory)
                    history.RemoveAt(0);
            }

            active = snapshot;
            wasActive = snapshot.Encounter.IsActive;
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
        }
    }
}
