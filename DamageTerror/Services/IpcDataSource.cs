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
    private bool connected;
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
            receiver = pluginInterface.GetIpcProvider<JObject, bool>("IINACT.IpcProvider.DamageTerror");
            receiver.RegisterFunc(OnDataReceived);

            sender = pluginInterface.GetIpcSubscriber<JObject, bool>("DamageTerror");

            var subscribeMsg = JObject.FromObject(new
            {
                call = "subscribe",
                events = new[] { "CombatData", "ChangePrimaryPlayer", "LogLine" },
            });

            try
            {
                sender.InvokeFunc(subscribeMsg);
                connected = true;
                    OnConnected?.Invoke();
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

                case "LogLine":
                    var lineArray = data["line"] as Newtonsoft.Json.Linq.JArray;
                    if (lineArray != null)
                    {
                        var fields = lineArray.Select(t => t.ToString()).ToArray();
                        OnLogLine?.Invoke(fields);
                    }
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
