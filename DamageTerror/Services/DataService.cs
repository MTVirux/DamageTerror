using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using DamageTerror.Helpers;

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
    private DateTime lastCombatDataTime;
    private readonly List<string[]> pendingLogLines = new();
#if DEBUG
    private readonly List<string> encounterLogLines = new();
#endif

    private const float PeriodicSaveInterval = 30f;
    private const double StalenessTimeoutSeconds = 15.0;

    public EncounterTimer EncounterTimer { get; } = new();
    public SkillTracker SkillTracker { get; }
    public GraphDataTracker GraphTracker { get; }
    public StatusTracker StatusTracker { get; }
    public EncounterStore Store { get; }
    public PositionalTable PositionalTable { get; }
    public string PlayerName { get; private set; } = string.Empty;
    public uint PlayerId { get; private set; }
    public bool IsConnected => activeSource?.IsConnected ?? false;
    public string ConnectionStatus { get; private set; } = "Not connected";

    /// <summary>Raised when a new encounter boundary is detected (active encounter starts).</summary>
    public event Action? OnNewEncounter;

    public DataService(IDalamudPluginInterface pluginInterface, IPluginLog log, Configuration config)
    {
        this.pluginInterface = pluginInterface;
        this.log = log;
        this.config = config;

        GraphTracker = new GraphDataTracker(log);
        GraphTracker.SetTimer(EncounterTimer);
        StatusTracker = new StatusTracker(ServiceManager.DataManager, log);
        StatusTracker.SetTimer(EncounterTimer);

        var configDir = pluginInterface.GetPluginConfigDirectory();

        PositionalTable = new PositionalTable(configDir, log);
        SkillTracker = new SkillTracker(ServiceManager.DataManager, log, PositionalTable);
        SkillTracker.SetDependencies(EncounterTimer, GraphTracker, StatusTracker);
        StatusTracker.SetSkillTracker(SkillTracker);

        Store = new EncounterStore(config);

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
                        if (restored.ItemEvents.Count > 0)
                            SkillTracker.SeedHistoricalItemEvents(restored.ItemEvents);
                    }

                    log.Debug($"Restored last encounter for {name} on startup");
                }
            }
        }
        catch (Exception ex) { log.Debug($"IPlayerState not available yet: {ex.Message}"); }
    }

    public async Task StartAsync()
    {
        if (disposed) return;

        // Initialize positional data from remote CSV (falls back to cache/embedded)
        await PositionalTable.InitializeAsync().ConfigureAwait(false);

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
        ws.OnConnected += () =>
        {
            ConnectionStatus = "Connected (WebSocket)";
            log.Information("WebSocket connected");
        };

        await ws.ConnectAsync(cts.Token).ConfigureAwait(false);

        activeSource = ws;

        if (!ws.IsConnected)
        {
            ConnectionStatus = "Waiting for IINACT (WebSocket reconnecting...)";
            log.Information("WebSocket not yet connected, auto-reconnect is active");
        }
    }

    public async Task ReconnectAsync()
    {
        Stop();
        await StartAsync().ConfigureAwait(false);
    }

    public void EndEncounter()
    {
        activeSource?.EndEncounter();
    }

    /// <summary>
    /// Cuts the current encounter: finalizes it, archives to history, resets all
    /// trackers, and tells IINACT to end so the next combat starts a fresh encounter.
    /// </summary>
    public void CutEncounter()
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

        activeSource?.EndEncounter();

        Store.ArchiveActive();
        Store.Save();

        SkillTracker.Reset();
        GraphTracker.Reset();
        StatusTracker.Reset();
        EncounterTimer.Reset();
        lastPeriodicSaveTime = 0f;
        wasActive = false;

        lock (pendingLogLines)
            pendingLogLines.Clear();
#if DEBUG
        lock (encounterLogLines)
            encounterLogLines.Clear();
#endif

        OnNewEncounter?.Invoke();
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

        // Mark any lingering active encounter as no longer live so the UI
        // doesn't keep showing stale active state after a disconnect.
        var active = Store.ActiveEncounter;
        if (active is { Encounter.IsActive: true })
        {
            active.Encounter.IsActive = false;
            wasActive = false;
        }
    }

    /// <summary>
    /// Checks whether the active encounter has gone stale (no CombatData received
    /// within <see cref="StalenessTimeoutSeconds"/>). If so, clears <c>IsActive</c>
    /// so the UI no longer shows it as live.
    /// Call from the render loop or a framework update handler.
    /// </summary>
    public void CheckStaleness()
    {
        if (disposed) return;
        if (Store.IsSampleDataActive) return;

        var active = Store.ActiveEncounter;
        if (active == null || !active.Encounter.IsActive) return;

        var elapsed = (DateTime.UtcNow - lastCombatDataTime).TotalSeconds;
        if (elapsed >= StalenessTimeoutSeconds)
        {
            active.Encounter.IsActive = false;
            wasActive = false;
        }
    }

    private void OnCombatData(EncounterSnapshot snapshot)
    {
        lastCombatDataTime = DateTime.UtcNow;

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
            catch (Exception ex) { log.Debug($"IPlayerState not available yet: {ex.Message}"); }
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
        var isNewEncounter = false;
        var isEncounterEnd = false;
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
#if DEBUG
            lock (encounterLogLines)
                encounterLogLines.Clear();
#endif

            isNewEncounter = true;
            OnNewEncounter?.Invoke();

            // Re-seed graph data so the line chart remains visible while
            // the live tracker accumulates its first few samples.
            if (outgoing != null)
            {
                if (outgoing.GraphData.Count > 0)
                    GraphTracker.SeedHistorical(outgoing.GraphData);
            }

            // Replay any log lines that arrived between the last CombatData
            // and this one so the first skill of a new encounter is not lost.
            DrainPendingLogLines();
        }

        // Detect encounter ending — combat was active and is now inactive.
        // Archive so it appears in history immediately.
        if (!snapshot.Encounter.IsActive && wasActive)
        {
            isEncounterEnd = true;
        }

        wasActive = snapshot.Encounter.IsActive;

        if (!isNewEncounter)
            DrainPendingLogLines();

        if (!string.IsNullOrEmpty(PlayerName))
            snapshot.PlayerName = PlayerName;

        // When a new encounter just started the tracker was reset, so the
        // active encounter in the store still holds the *previous* fight's
        // data.  Using it as a fallback would carry stale skill breakdowns
        // into the new encounter.  Null it out so the fresh tracker wins.
        var existing = isNewEncounter ? null : Store.ActiveEncounter;

        // Resolve home worlds from the current party list
        var worldMap = ResolvePartyWorldMap();

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

            // Derive heal count from tracked healing skills when the parser value is missing.
            var trackerHealCount = c.HealingSkills.Sum(s => s.HitCount);
            if (trackerHealCount > c.HealCount)
                c.HealCount = trackerHealCount;

            // Derive stun count from tracked Leg Sweep / Low Blow uses.
            var trackerStuns = SkillTracker.GetStunCount(c.Name);
            if (trackerStuns > c.Stuns)
                c.Stuns = trackerStuns;

            var trackerSkillIssue = SkillTracker.GetSkillIssueCount(c.Name);
            if (trackerSkillIssue > c.SkillIssue)
                c.SkillIssue = trackerSkillIssue;

            var trackerDamageDown = SkillTracker.GetDamageDownCount(c.Name);
            if (trackerDamageDown > c.DamageDown)
                c.DamageDown = trackerDamageDown;

            var trackerPositionalHits = SkillTracker.GetPositionalHits(c.Name);
            var trackerPositionalMisses = SkillTracker.GetPositionalMisses(c.Name);
            c.PositionalHits = Math.Max(c.PositionalHits, trackerPositionalHits);
            c.PositionalMisses = Math.Max(c.PositionalMisses, trackerPositionalMisses);
            c.Positionals = c.PositionalHits + c.PositionalMisses;

            if (!string.IsNullOrEmpty(PlayerName) && string.Equals(c.Name, PlayerName, StringComparison.OrdinalIgnoreCase))
                c.IsLocalPlayer = true;

            // Resolve home world: party list > existing entry > empty
            if (worldMap.TryGetValue(c.Name, out var world))
                c.HomeWorld = world;
            else if (!string.IsNullOrEmpty(existingEntry?.HomeWorld))
                c.HomeWorld = existingEntry!.HomeWorld;
        }

        var prev = existing;

        var archived = Store.Update(snapshot);

        GraphTracker.WindowSeconds = Math.Min(config.GraphSmoothingWindow, config.GraphViewSmoothingWindow);
        GraphTracker.SampleIntervalSeconds = Math.Min(config.GraphUpdateInterval, config.GraphViewUpdateInterval);
        GraphTracker.RecordSample(snapshot);

        // Feed sliding-window instant values back into each combatant entry
        // so columns, details panel, and tooltips show live iDPS / iHPS.
        foreach (var c in snapshot.Combatants)
        {
            var samples = GraphTracker.GetSamples(c.Name);
            if (samples.Count > 0)
            {
                var latest = samples[^1];
                c.InstantDps = latest.Dps;
                c.InstantHps = latest.Hps;
            }
        }

        // Track peak DPS as the highest instantaneous DPS achieved during the encounter.
        if (prev != null && !isNewEncounter)
        {
            foreach (var c in snapshot.Combatants)
            {
                var prevEntry = prev.Combatants.Find(p =>
                    string.Equals(p.Name, c.Name, StringComparison.OrdinalIgnoreCase));
                var prevPeak = prevEntry?.PeakDps ?? 0;
                c.PeakDps = Math.Max(c.InstantDps, prevPeak);
            }
        }
        else
        {
            foreach (var c in snapshot.Combatants)
                c.PeakDps = c.InstantDps;
        }

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

        // Copy the encounter into history now that Store.Update() has
        // applied it as active with the final snapshot data. Use
        // CopyActiveToHistory so the encounter stays visible in the
        // main window until the next encounter starts.
        if (isEncounterEnd)
        {
            var active = Store.ActiveEncounter;
            if (active != null)
                CaptureGraphData(active);

            if (Store.CopyActiveToHistory())
                Store.Save(force: true);
        }
    }

    private void CaptureGraphData(EncounterSnapshot target)
    {
#if DEBUG
        // Capture raw log lines accumulated during this encounter.
        lock (encounterLogLines)
        {
            if (encounterLogLines.Count > 0)
                target.RawLogLines = new List<string>(encounterLogLines);
        }
#endif

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

            var itemEvts = SkillTracker.GetItemEvents(c.Name);
            if (itemEvts.Count > 0)
                target.ItemEvents[c.Name] = itemEvts;

            var statusApplied = StatusTracker.GetStatusHistory(c.Name);
            if (statusApplied.Count > 0)
                target.StatusHistory[c.Name] = statusApplied;

            var statusReceived = StatusTracker.GetStatusesReceived(c.Name);
            if (statusReceived.Count > 0)
                target.StatusesReceived[c.Name] = statusReceived;
        }
    }

#if DEBUG
    /// <summary>
    /// Reprocess an encounter's raw log lines through the skill and status trackers,
    /// rebuilding per-combatant skill breakdowns, events, and status histories.
    /// This allows testing DoT/HoT attribution fixes on historical encounters.
    /// Only available in DEBUG builds since RawLogLines are only captured there.
    /// </summary>
    public void ReprocessEncounterLogLines(EncounterSnapshot snapshot)
    {
        if (snapshot.RawLogLines == null || snapshot.RawLogLines.Count == 0)
            return;

        // Reset trackers to a clean state for replay.
        SkillTracker.Reset();
        GraphTracker.Reset();
        StatusTracker.Reset();
        EncounterTimer.Restart();

        // Replay all raw log lines through the processing pipeline.
        foreach (var rawLine in snapshot.RawLogLines)
        {
            var fields = rawLine.Split('|');
            SkillTracker.ProcessLogLine(fields);
        }

        // Rebuild per-combatant skill lists from the replayed data.
        foreach (var c in snapshot.Combatants)
        {
            var newSkills = SkillTracker.GetSkills(c.Name);
            var newHealSkills = SkillTracker.GetHealSkills(c.Name);

            // Only overwrite if the replayed data has comparable or better totals.
            var newDmg = newSkills.Sum(s => s.TotalDamage);
            if (newDmg > 0)
                c.Skills = newSkills;

            var newHeal = newHealSkills.Sum(s => s.TotalDamage);
            if (newHeal > 0)
                c.HealingSkills = newHealSkills;

            // Refresh stun/debuff counts.
            var trackerStuns = SkillTracker.GetStunCount(c.Name);
            if (trackerStuns > 0) c.Stuns = trackerStuns;
            var trackerSI = SkillTracker.GetSkillIssueCount(c.Name);
            if (trackerSI > 0) c.SkillIssue = trackerSI;
            var trackerDD = SkillTracker.GetDamageDownCount(c.Name);
            if (trackerDD > 0) c.DamageDown = trackerDD;

            var trackerPH = SkillTracker.GetPositionalHits(c.Name);
            var trackerPM = SkillTracker.GetPositionalMisses(c.Name);
            if (trackerPH + trackerPM > 0)
            {
                c.PositionalHits = trackerPH;
                c.PositionalMisses = trackerPM;
                c.Positionals = trackerPH + trackerPM;
            }

            // Rebuild events and status histories.
            var events = SkillTracker.GetSkillEvents(c.Name);
            if (events.Count > 0)
                snapshot.SkillEvents[c.Name] = events;

            var dtEvents = SkillTracker.GetDamageTakenEvents(c.Name);
            if (dtEvents.Count > 0)
                snapshot.DamageTakenEvents[c.Name] = dtEvents;

            var itemEvts = SkillTracker.GetItemEvents(c.Name);
            if (itemEvts.Count > 0)
                snapshot.ItemEvents[c.Name] = itemEvts;

            var statusApplied = StatusTracker.GetStatusHistory(c.Name);
            if (statusApplied.Count > 0)
                snapshot.StatusHistory[c.Name] = statusApplied;

            var statusReceived = StatusTracker.GetStatusesReceived(c.Name);
            if (statusReceived.Count > 0)
                snapshot.StatusesReceived[c.Name] = statusReceived;
        }

        // Reset trackers again so they don't interfere with live encounters.
        SkillTracker.Reset();
        GraphTracker.Reset();
        StatusTracker.Reset();
    }
#endif

    /// <summary>
    /// Build a combatant name → home world name map from the current party list.
    /// </summary>
    private Dictionary<string, string> ResolvePartyWorldMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var members = ECommons.PartyFunctions.UniversalParty.Members;
            foreach (var member in members)
            {
                if (string.IsNullOrEmpty(member.Name)) continue;
                var worldName = member.HomeWorld.ValueNullable?.Name.ToString();
                if (!string.IsNullOrEmpty(worldName))
                    map[member.Name] = worldName;
            }
        }
        catch { /* Expected when not on main thread */ }

        return map;
    }

    private void DrainPendingLogLines()
    {
        string[][] snapshot;
        lock (pendingLogLines)
        {
            snapshot = pendingLogLines.ToArray();
            pendingLogLines.Clear();
        }

        foreach (var line in snapshot)
        {
            SkillTracker.ProcessLogLine(line);
#if DEBUG
            lock (encounterLogLines)
                encounterLogLines.Add(string.Join("|", line));
#endif
        }
    }

    private void OnLogLine(string[] line)
    {
        lock (pendingLogLines)
            pendingLogLines.Add(line);

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
#if DEBUG
        lock (encounterLogLines)
            encounterLogLines.Clear();
#endif

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
        PositionalTable.Dispose();
    }
}
