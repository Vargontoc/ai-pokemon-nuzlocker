using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace es.vargontoc.nuzlocke.ai.WebSockets;

public class AdviceConnectionManager : IAdviceConnectionManager
{
    private readonly ConcurrentDictionary<string, ConnectionEntry> _connections = new();
    private readonly ILogger<AdviceConnectionManager> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AdviceConnectionManager(ILogger<AdviceConnectionManager> logger)
    {
        _logger = logger;
    }

    public void AddConnection(string sessionId, WebSocket socket)
    {
        var entry = new ConnectionEntry(socket, new SemaphoreSlim(1, 1));

        if (_connections.TryRemove(sessionId, out var old))
        {
            old.SendLock.Dispose();
            _logger.LogInformation("Replaced existing WebSocket connection for session {SessionId}", sessionId);
        }

        _connections[sessionId] = entry;
        _logger.LogInformation("WebSocket connected for session {SessionId}", sessionId);
    }

    public void RemoveConnection(string sessionId)
    {
        if (_connections.TryRemove(sessionId, out var entry))
        {
            entry.SendLock.Dispose();
            _logger.LogInformation("WebSocket disconnected for session {SessionId}", sessionId);
        }
    }

    public bool HasConnection(string sessionId)
    {
        return _connections.TryGetValue(sessionId, out var entry)
            && entry.Socket.State == WebSocketState.Open;
    }

    public async Task<bool> SendAsync(string sessionId, object message, CancellationToken ct = default)
    {
        if (!_connections.TryGetValue(sessionId, out var entry))
            return false;

        if (entry.Socket.State != WebSocketState.Open)
        {
            RemoveConnection(sessionId);
            return false;
        }

        await entry.SendLock.WaitAsync(ct);
        try
        {
            var json = JsonSerializer.Serialize<object>(message, _jsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            await entry.Socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: ct);
            return true;
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning(ex, "WebSocket send failed for session {SessionId}", sessionId);
            RemoveConnection(sessionId);
            return false;
        }
        catch (ObjectDisposedException)
        {
            RemoveConnection(sessionId);
            return false;
        }
        finally
        {
            try { entry.SendLock.Release(); }
            catch (ObjectDisposedException) { }
        }
    }

    private record ConnectionEntry(WebSocket Socket, SemaphoreSlim SendLock);
}
