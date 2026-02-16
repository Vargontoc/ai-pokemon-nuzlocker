using System.Net.WebSockets;

namespace es.vargontoc.nuzlocke.ai.WebSockets;

public interface IAdviceConnectionManager
{
    void AddConnection(string sessionId, WebSocket socket);
    void RemoveConnection(string sessionId);
    bool HasConnection(string sessionId);
    Task<bool> SendAsync(string sessionId, AdviceWebSocketMessage message, CancellationToken ct = default);
}
