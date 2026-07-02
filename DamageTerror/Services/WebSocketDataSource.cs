using System.Net.WebSockets;
using System.Text;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DamageTerror.Services;

public sealed class WebSocketDataSource : IDataSource
{
    private const int InitialRetryDelayMs = 1000;
    private const int MaxRetryDelayMs = 30000;
    private const int MaxMessageSize = 10 * 1024 * 1024;

    private readonly IPluginLog log;
    private readonly string url;
    private ClientWebSocket? ws;
    private CancellationTokenSource? cts;
    private Task? receiveTask;
    private volatile bool disposed;

    public event Action<EncounterSnapshot>? OnCombatData;
    public event Action<string, uint>? OnPrimaryPlayerChanged;
    public event Action<string[]>? OnLogLine;
    public event Action<JObject>? OnRawCombatData;
    public event Action? OnConnected;

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
            await ConnectOnceAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            log.Debug($"WebSocket initial connection failed, will retry: {ex.Message}");
        }

        receiveTask = Task.Run(() => ReceiveAndReconnectLoopAsync(cts.Token), cts.Token);
    }

    private async Task ConnectOnceAsync(CancellationToken ct)
    {
        ws?.Dispose();
        var newWs = new ClientWebSocket();
        try
        {
            await newWs.ConnectAsync(new Uri(url), ct).ConfigureAwait(false);
        }
        catch
        {
            newWs.Dispose();
            throw;
        }

        ws = newWs;
        log.Information($"WebSocket connected to {url}");
        OnConnected?.Invoke();

        var subscribeMsg = JsonConvert.SerializeObject(new
        {
            call = "subscribe",
            events = new[] { "CombatData", "ChangePrimaryPlayer", "LogLine" },
        });
        var bytes = Encoding.UTF8.GetBytes(subscribeMsg);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct)
            .ConfigureAwait(false);
        log.Debug("Subscribed to CombatData, ChangePrimaryPlayer and LogLine events");
    }

    private async Task ReceiveAndReconnectLoopAsync(CancellationToken ct)
    {
        int retryDelay = InitialRetryDelayMs;

        while (!ct.IsCancellationRequested && !disposed)
        {
            var local = ws;
            if (local?.State == WebSocketState.Open)
            {
                retryDelay = InitialRetryDelayMs;
                try
                {
                    await ReceiveLoopAsync(local, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Without this, an unexpected throw from ReceiveAsync
                    // (e.g. WebSocketException on an abrupt server close) would
                    // tear down the receiveTask and leave the meter offline
                    // forever — the exception would never surface anywhere.
                    log.Debug($"WebSocket receive loop failed, will reconnect: {ex.Message}");
                }
            }

            if (ct.IsCancellationRequested || disposed)
                break;

            log.Debug($"WebSocket disconnected, reconnecting in {retryDelay}ms...");
            try
            {
                await Task.Delay(retryDelay, ct).ConfigureAwait(false);
                retryDelay = Math.Min(retryDelay * 2, MaxRetryDelayMs);
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

    private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];
        var messageBuilder = new StringBuilder();

        while (!ct.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            messageBuilder.Clear();
            WebSocketReceiveResult result;

            do
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    log.Information("WebSocket server closed connection");
                    return;
                }

                messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                if (messageBuilder.Length > MaxMessageSize)
                {
                    log.Warning("WebSocket message exceeded maximum size, discarding");
                    messageBuilder.Clear();
                    break;
                }
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
            DataSourceDispatcher.Dispatch(data, OnCombatData, OnPrimaryPlayerChanged, OnLogLine, OnRawCombatData);
        }
        catch (JsonException ex)
        {
            log.Debug($"Failed to parse WebSocket message: {ex.Message}");
        }
    }

    public void Disconnect()
    {
        // Cancel first so the receive loop wakes up and unwinds before we
        // touch the socket — otherwise ReceiveAsync would throw on a disposed
        // ClientWebSocket inside the catch block of ReceiveAndReconnectLoopAsync.
        try { cts?.Cancel(); }
        catch (Exception ex) { log.Debug($"WebSocket cancel error: {ex.Message}"); }

        // Capture and null the field so concurrent readers see "no socket"
        // immediately. We then close+dispose the captured local off the UI
        // thread, since CloseAsync's handshake can hang for the full 2s
        // timeout when the server has gone away ungracefully.
        var local = ws;
        ws = null;
        if (local == null)
            return;

        if (local.State == WebSocketState.Open)
        {
            _ = local.CloseAsync(WebSocketCloseStatus.NormalClosure, "Plugin closing", CancellationToken.None)
                .ContinueWith(t =>
                {
                    if (t.Exception != null)
                        log.Debug($"WebSocket close completed with error: {t.Exception.GetBaseException().Message}");
                    local.Dispose();
                }, TaskScheduler.Default);
        }
        else
        {
            local.Dispose();
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
