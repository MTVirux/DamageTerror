using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace DamageTerror.Services;

/// <summary>
/// Orchestrates data sources (IPC primary, WebSocket fallback) and maintains the encounter store.
/// </summary>
public class DataService : IDisposable
{
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IPluginLog log;
    private readonly Configuration config;
    private IDataSource? activeSource;
    private CancellationTokenSource? cts;
    private bool disposed;

    /// <summary>
    /// The encounter store containing active and historical encounters.
    /// </summary>
    public EncounterStore Store { get; }

    /// <summary>
    /// The resolved primary player name (replaces "YOU" in combat data).
    /// </summary>
    public string PlayerName { get; private set; } = string.Empty;

    /// <summary>
    /// The primary player's actor ID.
    /// </summary>
    public uint PlayerId { get; private set; }

    /// <summary>
    /// Whether the data service is currently connected to a data source.
    /// </summary>
    public bool IsConnected => activeSource?.IsConnected ?? false;

    /// <summary>
    /// A human-readable status string for the UI.
    /// </summary>
    public string ConnectionStatus { get; private set; } = "Not connected";

    /// <summary>
    /// The plugin configuration.
    /// </summary>
    public Configuration Config => config;

    public DataService(IDalamudPluginInterface pluginInterface, IPluginLog log, Configuration config)
    {
        this.pluginInterface = pluginInterface;
        this.log = log;
        this.config = config;
        Store = new EncounterStore(config.MaxEncounterHistory);
    }

    /// <summary>
    /// Start the data service. Tries IPC first (if preferred), then falls back to WebSocket.
    /// </summary>
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
            await ipc.ConnectAsync(cts.Token).ConfigureAwait(false);

            if (ipc.IsConnected)
            {
                activeSource = ipc;
                ConnectionStatus = "Connected (IPC)";
                log.Information("[DamageTerror] Using IPC data source");
                return;
            }

            // IPC failed, clean up and fall through to WebSocket
            ipc.OnCombatData -= OnCombatData;
            ipc.OnPrimaryPlayerChanged -= OnPrimaryPlayerChanged;
            ipc.Dispose();
            log.Information("[DamageTerror] IPC unavailable, falling back to WebSocket");
        }

        // WebSocket fallback
        await ConnectWebSocketAsync().ConfigureAwait(false);
    }

    private async Task ConnectWebSocketAsync()
    {
        if (disposed || cts == null) return;

        ConnectionStatus = "Connecting via WebSocket...";
        var ws = new WebSocketDataSource(config.WebSocketUrl, log);
        ws.OnCombatData += OnCombatData;
        ws.OnPrimaryPlayerChanged += OnPrimaryPlayerChanged;

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
            ws.Dispose();
            ConnectionStatus = "Not connected — IINACT not running?";
            log.Warning("[DamageTerror] Failed to connect to any data source");
        }
    }

    /// <summary>
    /// Attempt to reconnect to the data source.
    /// </summary>
    public async Task ReconnectAsync()
    {
        Stop();
        await StartAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Stop the data service and disconnect.
    /// </summary>
    public void Stop()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;

        if (activeSource != null)
        {
            activeSource.OnCombatData -= OnCombatData;
            activeSource.OnPrimaryPlayerChanged -= OnPrimaryPlayerChanged;
            activeSource.Dispose();
            activeSource = null;
        }

        ConnectionStatus = "Disconnected";
    }

    private void OnCombatData(EncounterSnapshot snapshot)
    {
        // Replace "YOU" with actual player name if known
        if (!string.IsNullOrEmpty(PlayerName))
        {
            foreach (var c in snapshot.Combatants)
            {
                if (string.Equals(c.Name, "YOU", StringComparison.OrdinalIgnoreCase))
                    c.Name = PlayerName;
            }
        }

        Store.Update(snapshot);
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
    }
}
