using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using Newtonsoft.Json.Linq;

namespace DamageTerror.Services;

/// <summary>
/// Connects to IINACT via Dalamud IPC (inter-plugin communication).
/// Uses the OverlayPlugin IPC naming convention:
///   - Provider (receive): IINACT.IpcProvider.DamageTerror
///   - Subscriber (send): DamageTerror
///
/// Note: This requires IINACT to register our IPC handler via
/// IpcHandlerController.CreateSubscriber("DamageTerror").
/// If IINACT doesn't expose a public API for registration, we fall back to WebSocket.
/// </summary>
public class IpcDataSource : IDataSource
{
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IPluginLog log;
    private ICallGateProvider<JObject, bool>? receiver;
    private ICallGateSubscriber<JObject, bool>? sender;
    private bool connected;
    private bool disposed;

    public event Action<EncounterSnapshot>? OnCombatData;
    public event Action<string, uint>? OnPrimaryPlayerChanged;

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
            // Register our IPC provider (IINACT sends data to us via this)
            receiver = pluginInterface.GetIpcProvider<JObject, bool>("IINACT.IpcProvider.DamageTerror");
            receiver.RegisterFunc(OnDataReceived);

            // Get the subscriber (we send messages to IINACT via this)
            sender = pluginInterface.GetIpcSubscriber<JObject, bool>("DamageTerror");

            // Send subscribe message to IINACT
            var subscribeMsg = JObject.FromObject(new
            {
                call = "subscribe",
                events = new[] { "CombatData", "ChangePrimaryPlayer" },
            });

            try
            {
                sender.InvokeFunc(subscribeMsg);
                connected = true;
                log.Information("[DamageTerror] IPC connected to IINACT");
            }
            catch (Exception ex)
            {
                log.Debug($"[DamageTerror] IPC subscribe call failed (IINACT may not be running): {ex.Message}");
                connected = false;
            }
        }
        catch (Exception ex)
        {
            log.Debug($"[DamageTerror] IPC registration failed: {ex.Message}");
            connected = false;
        }

        return Task.CompletedTask;
    }

    private bool OnDataReceived(JObject data)
    {
        try
        {
            var type = data["type"]?.ToString();

            switch (type)
            {
                case "CombatData":
                    var snapshot = CombatDataParser.Parse(data);
                    if (snapshot != null)
                        OnCombatData?.Invoke(snapshot);
                    break;

                case "ChangePrimaryPlayer":
                    var charName = data["charName"]?.ToString() ?? string.Empty;
                    var charId = data["charID"]?.ToObject<uint>() ?? 0;
                    if (!string.IsNullOrEmpty(charName))
                        OnPrimaryPlayerChanged?.Invoke(charName, charId);
                    break;
            }
        }
        catch (Exception ex)
        {
            log.Debug($"[DamageTerror] IPC message processing failed: {ex.Message}");
        }

        return true;
    }

    public void Disconnect()
    {
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
