using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using Newtonsoft.Json.Linq;

namespace DamageTerror.Services;

public class IpcDataSource : IDataSource
{
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IPluginLog log;
    private ICallGateProvider<JObject, bool>? receiver;
    private ICallGateSubscriber<JObject, bool>? sender;
    private volatile bool connected;
    private bool disposed;

    public event Action<EncounterSnapshot>? OnCombatData;
    public event Action<string, uint>? OnPrimaryPlayerChanged;
    public event Action<string[]>? OnLogLine;
    public event Action? OnConnected;

    public bool IsConnected => connected;

    public IpcDataSource(IDalamudPluginInterface pluginInterface, IPluginLog log)
    {
        this.pluginInterface = pluginInterface;
        this.log = log;
    }

    public Task ConnectAsync(CancellationToken ct = default)
    {
        if (disposed)
            return Task.CompletedTask;

        try
        {
            // Register our provider gate where IINACT will send events to us.
            receiver = pluginInterface.GetIpcProvider<JObject, bool>("DamageTerror");
            receiver.RegisterFunc(OnDataReceived);

            // Ask IINACT to create its handler pair for "DamageTerror".
            var createSub = pluginInterface.GetIpcSubscriber<string, bool>("IINACT.CreateSubscriber");
            createSub.InvokeFunc("DamageTerror");

            // Subscribe to the command gate IINACT created for us.
            sender = pluginInterface.GetIpcSubscriber<JObject, bool>("IINACT.IpcProvider.DamageTerror");

            var subscribeMsg = JObject.FromObject(new
            {
                call = "subscribe",
                events = new[] { "CombatData", "ChangePrimaryPlayer", "LogLine" },
            });

            try
            {
                sender.InvokeAction(subscribeMsg);
                connected = true;
                OnConnected?.Invoke();
                log.Information("IPC connected to IINACT");
            }
            catch (Exception ex)
            {
                log.Debug($"IPC subscribe call failed (IINACT may not be running): {ex.Message}");
                connected = false;
            }
        }
        catch (Exception ex)
        {
            log.Debug($"IPC registration failed: {ex.Message}");
            connected = false;
        }

        return Task.CompletedTask;
    }

    private bool OnDataReceived(JObject data)
    {
        try
        {
            DataSourceDispatcher.Dispatch(data, OnCombatData, OnPrimaryPlayerChanged, OnLogLine);
        }
        catch (Exception ex)
        {
            log.Debug($"IPC message processing failed: {ex.Message}");
        }

        return true;
    }

    public void Disconnect()
    {
        try
        {
            var unsub = pluginInterface.GetIpcSubscriber<string, bool>("IINACT.Unsubscribe");
            unsub.InvokeFunc("DamageTerror");
        }
        catch
        {
            // IINACT may already be gone
        }

        try
        {
            receiver?.UnregisterFunc();
        }
        catch
        {
            // Ignore unregister errors
        }

        connected = false;
    }

    public void EndEncounter()
    {
        if (!connected || sender == null)
            return;

        try
        {
            var endMsg = JObject.FromObject(new { call = "end" });
            sender.InvokeAction(endMsg);
            log.Debug("Sent end encounter command via IPC");
        }
        catch (Exception ex)
        {
            log.Debug($"Failed to send end encounter via IPC: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Disconnect();
    }
}
