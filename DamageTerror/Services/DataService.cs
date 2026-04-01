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

    public SkillTracker SkillTracker { get; } = new(ServiceManager.DataManager);
    public EncounterStore Store { get; }
    public string PlayerName { get; private set; } = string.Empty;
    public uint PlayerId { get; private set; }
    public bool IsConnected => activeSource?.IsConnected ?? false;
    public string ConnectionStatus { get; private set; } = "Not connected";
    public Configuration Config => config;

    public DataService(IDalamudPluginInterface pluginInterface, IPluginLog log, Configuration config)
    {
        this.pluginInterface = pluginInterface;
        this.log = log;
        this.config = config;
        Store = new EncounterStore(config.MaxEncounterHistory);

        var configDir = pluginInterface.GetPluginConfigDirectory();
        var savePath = System.IO.Path.Combine(configDir, "encounters.json");
        Store.SetSavePath(savePath);
        Store.Load();
        log.Debug($"[DamageTerror] Encounter history loaded from {savePath}");
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
                log.Information("[DamageTerror] Using IPC data source");
            }

            ipc.OnConnected += ConnectedHandler;

            await ipc.ConnectAsync(cts.Token).ConfigureAwait(false);

            if (ipc.IsConnected)
            {
                ipc.OnConnected -= ConnectedHandler;
                activeSource = ipc;
                ConnectionStatus = "Connected (IPC)";
                log.Information("[DamageTerror] Using IPC data source");
                return;
            }

            ipc.OnConnected -= ConnectedHandler;
            ipc.OnCombatData -= OnCombatData;
            ipc.OnPrimaryPlayerChanged -= OnPrimaryPlayerChanged;
            ipc.OnLogLine -= OnLogLine;
            ipc.Dispose();
            log.Information("[DamageTerror] IPC unavailable, falling back to WebSocket");
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
            log.Information("[DamageTerror] Using WebSocket data source");
        }
        else
        {
            ws.OnCombatData -= OnCombatData;
            ws.OnPrimaryPlayerChanged -= OnPrimaryPlayerChanged;
            ws.OnLogLine -= OnLogLine;
            ws.Dispose();
            ConnectionStatus = "Not connected — IINACT not running?";
            log.Warning("[DamageTerror] Failed to connect to any data source");
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
                        log.Debug($"[DamageTerror] Player name from IPlayerState: {name}");
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
            }

            SkillTracker.Reset();
        }
        wasActive = snapshot.Encounter.IsActive;

        foreach (var c in snapshot.Combatants)
        {
            c.Skills = SkillTracker.GetSkills(c.Name);
            c.HealingSkills = SkillTracker.GetHealSkills(c.Name);
            if (!string.IsNullOrEmpty(PlayerName) && string.Equals(c.Name, PlayerName, StringComparison.OrdinalIgnoreCase))
                c.IsLocalPlayer = true;
        }

        var archived = Store.Update(snapshot);

        if (archived)
            Store.Save();
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
                PlayerName = line[3];
                PlayerId = id;
                log.Debug($"[DamageTerror] Player name from LogLine: {line[3]} (ID: {id})");
            }
        }
    }

    private void OnPrimaryPlayerChanged(string name, uint id)
    {
        PlayerName = name;
        PlayerId = id;
        log.Debug($"[DamageTerror] Primary player set: {name} (ID: {id})");
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Stop();
        Store.Save(force: true);
    }
}
