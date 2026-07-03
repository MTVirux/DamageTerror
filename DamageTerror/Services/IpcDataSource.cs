using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace DamageTerror.Services;

public sealed class IpcDataSource : IDataSource
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
    public event Action<JObject>? OnRawCombatData;
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
            // IINACT initializes OverlayIpcHandler inside a Task.Run, so this can
            // legitimately return false if DamageTerror loads first — falling
            // through would silently set connected=true and the meter would sit
            // forever waiting on a no-op subscribe.
            var createSub = pluginInterface.GetIpcSubscriber<string, bool>("IINACT.CreateSubscriber");
            if (!createSub.InvokeFunc("DamageTerror"))
            {
                log.Debug("IINACT.CreateSubscriber returned false; IINACT not ready, will retry");
                connected = false;
                return Task.CompletedTask;
            }

            // Subscribe to the command gate IINACT created for us.
            sender = pluginInterface.GetIpcSubscriber<JObject, bool>("IINACT.IpcProvider.DamageTerror");

            var subscribeMsg = JObject.FromObject(new
            {
                call = "subscribe",
                events = DataSourceDispatcher.SubscribedEvents,
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
            DataSourceDispatcher.Dispatch(data, OnCombatData, OnPrimaryPlayerChanged, OnLogLine, OnRawCombatData);
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

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Disconnect();
    }
}
