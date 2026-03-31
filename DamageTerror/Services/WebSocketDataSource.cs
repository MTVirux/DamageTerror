using System.Net.WebSockets;
using System.Text;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DamageTerror.Services;

public class WebSocketDataSource : IDataSource
{
    private readonly IPluginLog log;
    private readonly string url;
    private ClientWebSocket? ws;
    private CancellationTokenSource? cts;
    private Task? receiveTask;
    private bool disposed;

    public event Action<EncounterSnapshot>? OnCombatData;
    public event Action<string, uint>? OnPrimaryPlayerChanged;
    public event Action<string[]>? OnLogLine;

    public bool IsConnected => ws?.State == WebSocketState.Open;

    public WebSocketDataSource(string url, IPluginLog log)
    {
        this.url = url;
        this.log = log;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (disposed)
            return;

        cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri(url), cts.Token).ConfigureAwait(false);
            log.Information($"[DamageTerror] WebSocket connected to {url}");

            var subscribeMsg = JsonConvert.SerializeObject(new
            {
                call = "subscribe",
                events = new[] { "CombatData", "ChangePrimaryPlayer", "LogLine" },
            });
            var bytes = Encoding.UTF8.GetBytes(subscribeMsg);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token)
                .ConfigureAwait(false);
            log.Debug("[DamageTerror] Subscribed to CombatData and ChangePrimaryPlayer events");

            receiveTask = Task.Run(() => ReceiveLoopAsync(cts.Token), cts.Token);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            log.Warning($"[DamageTerror] WebSocket connection failed: {ex.Message}");
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];
        var messageBuilder = new StringBuilder();

        while (!ct.IsCancellationRequested && ws?.State == WebSocketState.Open)
        {
            try
            {
                messageBuilder.Clear();
                WebSocketReceiveResult result;

                do
                {
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        log.Information("[DamageTerror] WebSocket server closed connection");
                        return;
                    }

                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                var message = messageBuilder.ToString();
                ProcessMessage(message);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (WebSocketException ex)
            {
                log.Warning($"[DamageTerror] WebSocket error: {ex.Message}");
                break;
            }
        }
    }

    private void ProcessMessage(string message)
    {
        try
        {
            var data = JObject.Parse(message);
            DataSourceDispatcher.Dispatch(data, OnCombatData, OnPrimaryPlayerChanged, OnLogLine);
        }
        catch (JsonException ex)
        {
            log.Debug($"[DamageTerror] Failed to parse WebSocket message: {ex.Message}");
        }
    }

    public void Disconnect()
    {
        try
        {
            cts?.Cancel();
            if (ws?.State == WebSocketState.Open)
            {
                // Best-effort close
                ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Plugin closing",
                    CancellationToken.None).Wait(TimeSpan.FromSeconds(2));
            }
        }
        catch
        {
            // Ignore close errors
        }
        finally
        {
            ws?.Dispose();
            ws = null;
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Disconnect();
        cts?.Dispose();
    }
}
