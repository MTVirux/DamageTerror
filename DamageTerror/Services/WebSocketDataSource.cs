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
    private volatile bool disposed;

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

        await ConnectOnceAsync(cts.Token).ConfigureAwait(false);

        receiveTask = Task.Run(() => ReceiveAndReconnectLoopAsync(cts.Token), cts.Token);
    }

    private async Task ConnectOnceAsync(CancellationToken ct)
    {
        ws?.Dispose();
        ws = new ClientWebSocket();

        await ws.ConnectAsync(new Uri(url), ct).ConfigureAwait(false);
        log.Information($"WebSocket connected to {url}");

        var subscribeMsg = JsonConvert.SerializeObject(new
        {
            call = "subscribe",
            events = new[] { "CombatData", "ChangePrimaryPlayer", "LogLine" },
        });
        var bytes = Encoding.UTF8.GetBytes(subscribeMsg);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct)
            .ConfigureAwait(false);
        log.Debug("Subscribed to CombatData and ChangePrimaryPlayer events");
    }

    private async Task ReceiveAndReconnectLoopAsync(CancellationToken ct)
    {
        int retryDelay = 1000;
        const int maxDelay = 30000;

        while (!ct.IsCancellationRequested && !disposed)
        {
            if (ws?.State == WebSocketState.Open)
            {
                retryDelay = 1000;
                await ReceiveLoopAsync(ct).ConfigureAwait(false);
            }

            if (ct.IsCancellationRequested || disposed)
                break;

            log.Debug($"WebSocket disconnected, reconnecting in {retryDelay}ms...");
            try
            {
                await Task.Delay(retryDelay, ct).ConfigureAwait(false);
                retryDelay = Math.Min(retryDelay * 2, maxDelay);
                await ConnectOnceAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                log.Debug($"WebSocket reconnect failed: {ex.Message}");
            }
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];
        var messageBuilder = new StringBuilder();

        while (!ct.IsCancellationRequested && ws?.State == WebSocketState.Open)
        {
            messageBuilder.Clear();
            WebSocketReceiveResult result;

            do
            {
                result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    log.Information("WebSocket server closed connection");
                    return;
                }

                messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
            while (!result.EndOfMessage);

            var message = messageBuilder.ToString();
            ProcessMessage(message);
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
            log.Debug($"Failed to parse WebSocket message: {ex.Message}");
        }
    }

    public void Disconnect()
    {
        try
        {
            cts?.Cancel();
            if (ws?.State == WebSocketState.Open)
            {
                ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Plugin closing",
                    CancellationToken.None).Wait(TimeSpan.FromSeconds(2));
            }
        }
        catch (Exception ex)
        {
            ServiceManager.PluginLog.Debug($"WebSocket disconnect error: {ex.Message}");
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
