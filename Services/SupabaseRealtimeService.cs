using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using NodeHiveCenter.Models;

namespace NodeHiveCenter.Services;

/// <summary>
/// Connects to Supabase Realtime via Phoenix WebSocket protocol.
/// Subscribes to UPDATE changes on public.session where id=1.
/// </summary>
public class SupabaseRealtimeService
{
    private const string AnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imd5cXpja3lqeGxjcXFkZnVnY2dnIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzczNjIyOTAsImV4cCI6MjA5MjkzODI5MH0.bn53GTMchXySu9pYlV87RvL93mmsaL3s3RfYxrDbVjE";
    private const string WsUrl   = $"wss://gyqzckyjxlcqqdfugcgg.supabase.co/realtime/v1/websocket?apikey={AnonKey}&vsn=1.0.0";
    private const string Channel = "realtime:public:session";

    public event Action? OnConnected;
    public event Action? OnDisconnected;
    public event Action<SessionState>? OnSessionUpdated;

    private ClientWebSocket? _ws;
    private CancellationTokenSource _cts = new();
    private int _refCounter = 0;

    public async Task StartAsync()
    {
        while (true)
        {
            try
            {
                await ConnectAndListenAsync();
            }
            catch { /* reconnect */ }

            OnDisconnected?.Invoke();
            await Task.Delay(3000); // reconnect delay
        }
    }

    private async Task ConnectAndListenAsync()
    {
        _cts = new CancellationTokenSource();
        _ws  = new ClientWebSocket();
        _ws.Options.SetRequestHeader("apikey", AnonKey);

        await _ws.ConnectAsync(new Uri(WsUrl), _cts.Token);
        OnConnected?.Invoke();

        // Join the channel with postgres_changes subscription
        await SendAsync(new
        {
            topic   = Channel,
            @event  = "phx_join",
            payload = new
            {
                config = new
                {
                    postgres_changes = new[]
                    {
                        new { @event = "UPDATE", schema = "public", table = "session", filter = "id=eq.1" }
                    }
                }
            },
            @ref = NextRef()
        });

        // Start heartbeat
        _ = HeartbeatLoopAsync();

        // Receive loop
        var buffer = new byte[16384];
        var sb     = new StringBuilder();

        while (_ws.State == WebSocketState.Open)
        {
            sb.Clear();
            WebSocketReceiveResult result;
            do
            {
                result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                if (result.MessageType == WebSocketMessageType.Close) return;
                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
            while (!result.EndOfMessage);

            ProcessMessage(sb.ToString());
        }
    }

    private void ProcessMessage(string raw)
    {
        try
        {
            var doc = JsonNode.Parse(raw);
            if (doc == null) return;

            var ev = doc["event"]?.GetValue<string>();

            // Handle postgres_changes broadcast
            if (ev == "postgres_changes")
            {
                var record = doc["payload"]?["data"]?["record"];
                if (record == null) return;

                var session = new SessionState
                {
                    Id          = record["id"]?.GetValue<int>()             ?? 0,
                    Status      = record["status"]?.GetValue<string>()      ?? "",
                    Prompt      = record["prompt"]?.GetValue<string>(),
                    Result      = record["result"]?.GetValue<string>(),
                    NodeAStatus = record["node_a_status"]?.GetValue<string>(),
                    NodeBStatus = record["node_b_status"]?.GetValue<string>()
                };
                OnSessionUpdated?.Invoke(session);
                return;
            }

            // Supabase Realtime v2 wraps events inside system messages
            // Also handle "broadcast" with type postgres_changes
            var type = doc["payload"]?["type"]?.GetValue<string>();
            if (type == "broadcast")
            {
                var innerEv = doc["payload"]?["event"]?.GetValue<string>();
                if (innerEv == "postgres_changes")
                {
                    var record = doc["payload"]?["payload"]?["data"]?["record"];
                    if (record == null) return;
                    var session = new SessionState
                    {
                        Id          = record["id"]?.GetValue<int>()             ?? 0,
                        Status      = record["status"]?.GetValue<string>()      ?? "",
                        Prompt      = record["prompt"]?.GetValue<string>(),
                        Result      = record["result"]?.GetValue<string>(),
                        NodeAStatus = record["node_a_status"]?.GetValue<string>(),
                        NodeBStatus = record["node_b_status"]?.GetValue<string>()
                    };
                    OnSessionUpdated?.Invoke(session);
                }
            }
        }
        catch { /* ignore malformed messages */ }
    }

    private async Task HeartbeatLoopAsync()
    {
        while (_ws?.State == WebSocketState.Open)
        {
            await Task.Delay(25000);
            if (_ws?.State != WebSocketState.Open) break;
            try
            {
                await SendAsync(new
                {
                    topic   = "phoenix",
                    @event  = "heartbeat",
                    payload = new { },
                    @ref    = NextRef()
                });
            }
            catch { break; }
        }
    }

    private async Task SendAsync(object payload)
    {
        if (_ws == null || _ws.State != WebSocketState.Open) return;
        var json  = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
    }

    private string NextRef() => (++_refCounter).ToString();
}
