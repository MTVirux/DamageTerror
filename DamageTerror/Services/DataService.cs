using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace DamageTerror.Services;

public class DataService : IDisposable
{
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IPluginLog log;
    private readonly Configuration config;
    private IDataSource? activeSource;
    private CancellationTokenSource? cts;
    private bool disposed;
    private bool wasActive;
    private bool playerChanged;
    private float lastPeriodicSaveTime;

    /// <summary>Interval in seconds between periodic graph data captures during active encounters.</summary>
    private const float PeriodicSaveInterval = 30f;

    public EncounterTimer EncounterTimer { get; } = new();
    public SkillTracker SkillTracker { get; }
    public GraphDataTracker GraphTracker { get; }
    public StatusTracker StatusTracker { get; }
    public EncounterStore Store { get; }
    public string PlayerName { get; private set; } = string.Empty;
    public uint PlayerId { get; private set; }
    public bool IsConnected => activeSource?.IsConnected ?? false;
    public string ConnectionStatus { get; private set; } = "Not connected";

    public DataService(IDalamudPluginInterface pluginInterface, IPluginLog log, Configuration config)
    {
        this.pluginInterface = pluginInterface;
        this.log = log;
        this.config = config;

        // Initialize services with shared timer for synchronized timestamps.
        GraphTracker = new GraphDataTracker(log);
        GraphTracker.SetTimer(EncounterTimer);
        StatusTracker = new StatusTracker(ServiceManager.DataManager, log);
        StatusTracker.SetTimer(EncounterTimer);
        SkillTracker = new SkillTracker(ServiceManager.DataManager, log);
        SkillTracker.SetDependencies(EncounterTimer, GraphTracker, StatusTracker);
        StatusTracker.SetSkillTracker(SkillTracker);

        Store = new EncounterStore(config.MaxEncounterHistory);

        var configDir = pluginInterface.GetPluginConfigDirectory();
        var savePath = System.IO.Path.Combine(configDir, "encounters.json");
        Store.SetSavePath(savePath);
        Store.Load();
        log.Debug($"Encounter history loaded from {savePath}");

        // Attempt to restore the most recent encounter for this player so
        // graph data is visible immediately after a plugin reload.
        try
        {
            var ps = ServiceManager.PlayerState;
            if (ps is { IsLoaded: true })
            {
                var name = ps.CharacterName;
                if (!string.IsNullOrEmpty(name))
                {
                    PlayerName = name;
                    Store.RestoreLatestForPlayer(name);

                    // Seed trackers with historical data from the restored encounter
                    // so graphs are visible immediately (survives snapshot replacement).
                    var restored = Store.ActiveEncounter;
                    if (restored != null)
                    {
                        if (restored.GraphData.Count > 0)
                            GraphTracker.SeedHistorical(restored.GraphData);
                        if (restored.SkillEvents.Count > 0)
                            SkillTracker.SeedHistoricalEvents(restored.SkillEvents);
                        if (restored.DamageTakenEvents.Count > 0)
                            SkillTracker.SeedHistoricalDamageTakenEvents(restored.DamageTakenEvents);
                    }

                    log.Debug($"Restored last encounter for {name} on startup");
                }
            }
        }
        catch { /* IPlayerState may not be available yet */ }
    }

    public async Task StartAsync()
    {
        if (disposed) return;

        cts = new CancellationTokenSource();

        if (config.PreferIpc)
        {
            ConnectionStatus = "Connecting via IPC...";
            var ipc = new IpcDataSource(pluginInterface, log);
            ipc.OnCombatData += OnCombatData;
            ipc.OnPrimaryPlayerChanged += OnPrimaryPlayerChanged;
            ipc.OnLogLine += OnLogLine;

            void ConnectedHandler()
            {
                activeSource = ipc;
                ConnectionStatus = "Connected (IPC)";
                log.Information("Using IPC data source");
            }

            ipc.OnConnected += ConnectedHandler;

            await ipc.ConnectAsync(cts.Token).ConfigureAwait(false);

            if (ipc.IsConnected)
            {
                ipc.OnConnected -= ConnectedHandler;
                activeSource = ipc;
                ConnectionStatus = "Connected (IPC)";
                log.Information("Using IPC data source");
                return;
            }

            ipc.OnConnected -= ConnectedHandler;
            ipc.OnCombatData -= OnCombatData;
            ipc.OnPrimaryPlayerChanged -= OnPrimaryPlayerChanged;
            ipc.OnLogLine -= OnLogLine;
            ipc.Dispose();
            log.Information("IPC unavailable, falling back to WebSocket");
        }

        await ConnectWebSocketAsync().ConfigureAwait(false);
    }

    private async Task ConnectWebSocketAsync()
    {
        if (disposed || cts == null) return;

        ConnectionStatus = "Connecting via WebSocket...";
        var ws = new WebSocketDataSource(config.WebSocketUrl, log);
        ws.OnCombatData += OnCombatData;
        ws.OnPrimaryPlayerChanged += OnPrimaryPlayerChanged;
        ws.OnLogLine += OnLogLine;

        await ws.ConnectAsync(cts.Token).ConfigureAwait(false);

        if (ws.IsConnected)
        {
            activeSource = ws;
            ConnectionStatus = "Connected (WebSocket)";
            log.Information("Using WebSocket data source");
        }
        else
        {
            ws.OnCombatData -= OnCombatData;
            ws.OnPrimaryPlayerChanged -= OnPrimaryPlayerChanged;
            ws.OnLogLine -= OnLogLine;
            ws.Dispose();
            ConnectionStatus = "Not connected — IINACT not running?";
            log.Warning("Failed to connect to any data source");
        }
    }

    public async Task ReconnectAsync()
    {
        Stop();
        await StartAsync().ConfigureAwait(false);
    }

    public void Stop()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;

        if (activeSource != null)
        {
            activeSource.OnCombatData -= OnCombatData;
            activeSource.OnPrimaryPlayerChanged -= OnPrimaryPlayerChanged;
            activeSource.OnLogLine -= OnLogLine;
            activeSource.Dispose();
            activeSource = null;
        }

        ConnectionStatus = "Disconnected";
    }

    private void OnCombatData(EncounterSnapshot snapshot)
    {
        // After a player change, drop stale data from the previous session
        // until a genuinely new active encounter starts.
        if (playerChanged)
        {
            if (!snapshot.Encounter.IsActive)
                return;
            playerChanged = false;
        }

        if (string.IsNullOrEmpty(PlayerName))
        {
            try
            {
                var ps = ServiceManager.PlayerState;
                if (ps is { IsLoaded: true })
                {
                    var name = ps.CharacterName;
                    if (!string.IsNullOrEmpty(name))
                    {
                        PlayerName = name;
                        log.Debug($"Player name from IPlayerState: {name}");
                    }
                }
            }
            catch { /* IPlayerState may not be available yet */ }
        }

        if (!string.IsNullOrEmpty(PlayerName))
        {
            foreach (var c in snapshot.Combatants)
            {
                if (string.Equals(c.Name, "YOU", StringComparison.OrdinalIgnoreCase))
                    c.Name = PlayerName;
            }
        }

        // Detect new encounter boundary — ensure the outgoing encounter
        // has a final skills snapshot before resetting the tracker.
        if (snapshot.Encounter.IsActive && !wasActive)
        {
            var outgoing = Store.ActiveEncounter;
            if (outgoing != null)
            {
                foreach (var c in outgoing.Combatants)
                {
                    c.Skills = SkillTracker.GetSkills(c.Name);
                    c.HealingSkills = SkillTracker.GetHealSkills(c.Name);
                }

                CaptureGraphData(outgoing);
            }

            SkillTracker.Reset();
            GraphTracker.Reset();
            StatusTracker.Reset();
            EncounterTimer.Restart();
            lastPeriodicSaveTime = 0f;

            // Re-seed historical data so the graph remains visible while
            // the live tracker accumulates its first few samples.
            if (outgoing != null)
            {
                if (outgoing.GraphData.Count > 0)
                    GraphTracker.SeedHistorical(outgoing.GraphData);
                if (outgoing.SkillEvents.Count > 0)
                    SkillTracker.SeedHistoricalEvents(outgoing.SkillEvents);
                if (outgoing.DamageTakenEvents.Count > 0)
                    SkillTracker.SeedHistoricalDamageTakenEvents(outgoing.DamageTakenEvents);
            }
        }

        wasActive = snapshot.Encounter.IsActive;

        if (!string.IsNullOrEmpty(PlayerName))
            snapshot.PlayerName = PlayerName;

        var existing = Store.ActiveEncounter;

        foreach (var c in snapshot.Combatants)
        {
            var trackerSkills = SkillTracker.GetSkills(c.Name);
            var trackerHealSkills = SkillTracker.GetHealSkills(c.Name);

            // Preserve existing skills from the active encounter when the tracker
            // has less data (e.g. after a plugin reload where the tracker restarted
            // but CombatData still has cumulative totals).
            var existingEntry = existing?.Combatants.Find(p =>
                string.Equals(p.Name, c.Name, StringComparison.OrdinalIgnoreCase));

            var trackerDmg = trackerSkills.Sum(s => s.TotalDamage);
            var existingDmg = existingEntry?.Skills?.Sum(s => s.TotalDamage) ?? 0;
            c.Skills = trackerDmg >= existingDmg ? trackerSkills : existingEntry!.Skills;

            var trackerHeal = trackerHealSkills.Sum(s => s.TotalDamage);
            var existingHeal = existingEntry?.HealingSkills?.Sum(s => s.TotalDamage) ?? 0;
            c.HealingSkills = trackerHeal >= existingHeal ? trackerHealSkills : existingEntry!.HealingSkills;

            if (!string.IsNullOrEmpty(PlayerName) && string.Equals(c.Name, PlayerName, StringComparison.OrdinalIgnoreCase))
                c.IsLocalPlayer = true;
        }

        var prev = existing;
        if (prev != null)
        {
            foreach (var c in snapshot.Combatants)
            {
                var prevEntry = prev.Combatants.Find(p =>
                    string.Equals(p.Name, c.Name, StringComparison.OrdinalIgnoreCase));
                var prevPeak = prevEntry?.PeakDps ?? 0;
                c.PeakDps = Math.Max(c.EncDps, prevPeak);
            }
        }
        else
        {
            foreach (var c in snapshot.Combatants)
                c.PeakDps = c.EncDps;
        }

        var archived = Store.Update(snapshot);

        GraphTracker.WindowSeconds = Math.Min(config.GraphSmoothingWindow, config.GraphViewSmoothingWindow);
        GraphTracker.SampleIntervalSeconds = Math.Min(config.GraphUpdateInterval, config.GraphViewUpdateInterval);
        GraphTracker.RecordSample(snapshot);

        // Periodically capture graph data during active encounters so that
        // at most ~30 seconds of data is lost on an unexpected shutdown.
        if (snapshot.Encounter.IsActive)
        {
            var elapsed = EncounterTimer.ElapsedSeconds;
            if (elapsed - lastPeriodicSaveTime >= PeriodicSaveInterval)
            {
                lastPeriodicSaveTime = elapsed;
                var active = Store.ActiveEncounter;
                if (active != null)
                {
                    CaptureGraphData(active);
                    Store.Save();
                }
            }
        }

        if (archived)
            Store.Save();
    }

    private void CaptureGraphData(EncounterSnapshot target)
    {
        // Only overwrite per-combatant entries where the tracker has data.
        // This preserves graph data loaded from disk when the tracker is empty
        // (e.g. after a plugin reload).
        foreach (var c in target.Combatants)
        {
            var samples = GraphTracker.GetSamples(c.Name);
            if (samples.Count > 0)
                target.GraphData[c.Name] = samples;

            var events = SkillTracker.GetSkillEvents(c.Name);
            if (events.Count > 0)
                target.SkillEvents[c.Name] = events;

            var dtEvents = SkillTracker.GetDamageTakenEvents(c.Name);
            if (dtEvents.Count > 0)
                target.DamageTakenEvents[c.Name] = dtEvents;
        }
    }

    private void OnLogLine(string[] line)
    {
        SkillTracker.ProcessLogLine(line);

        // Extract player name from LogLine type 02 (ChangePrimaryPlayer) as a
        // reliable fallback — the separate ChangePrimaryPlayer event is a cached
        // event in OverlayPlugin and may not be delivered before the first CombatData.
        if (line.Length >= 4 && line[0] == "02" && !string.IsNullOrEmpty(line[3]))
        {
            if (uint.TryParse(line[2], System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out var id))
            {
                OnPlayerChanged(line[3], id);
            }
        }
    }

    private void OnPlayerChanged(string newName, uint newId)
    {
        if (string.Equals(PlayerName, newName, StringComparison.OrdinalIgnoreCase))
        {
            PlayerName = newName;
            PlayerId = newId;
            return;
        }

        var outgoing = Store.ActiveEncounter;
        if (outgoing != null)
        {
            foreach (var c in outgoing.Combatants)
            {
                c.Skills = SkillTracker.GetSkills(c.Name);
                c.HealingSkills = SkillTracker.GetHealSkills(c.Name);
            }

            CaptureGraphData(outgoing);
        }

        SkillTracker.Reset();
        GraphTracker.Reset();
        StatusTracker.Reset();
        EncounterTimer.Reset();

        if (Store.ArchiveActive())
            Store.Save();

        wasActive = false;
        playerChanged = true;
        PlayerName = newName;
        PlayerId = newId;

        Store.RestoreLatestForPlayer(newName);
        log.Debug($"Player changed to: {newName} (ID: {newId})");
    }

    private void OnPrimaryPlayerChanged(string name, uint id)
    {
        OnPlayerChanged(name, id);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Stop();

        var outgoing = Store.ActiveEncounter;
        if (outgoing != null)
            CaptureGraphData(outgoing);

        Store.ArchiveActive();
        Store.Save(force: true);
    }
}
