using es.vargontoc.nuzlocke.ai.Workflows;

namespace es.vargontoc.nuzlocke.ai.WebSockets;

public class AdviceWebSocketMessage
{
    public required string Type { get; set; }
    public required string CorrelationId { get; set; }
}

public class AdviceStartMessage : AdviceWebSocketMessage
{
    public required string WorkflowId { get; set; }
}

public class AdviceChunkMessage : AdviceWebSocketMessage
{
    public required string Content { get; set; }
}

public class AdviceEndMessage : AdviceWebSocketMessage
{
    public required string FullAdvice { get; set; }
}

public class AdviceErrorMessage : AdviceWebSocketMessage
{
    public required string Error { get; set; }
}

public class WorkflowEventMessage : AdviceWebSocketMessage
{
    public required string WorkflowId { get; set; }
    public required bool Success { get; set; }
    public List<StateMutation> Mutations { get; set; } = new();
    public Dictionary<string, object?> Data { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class AdviceDispatchRequest
{
    public required string CorrelationId { get; set; }
    public required string NuzlockeId { get; set; }
    public required string WorkflowId { get; set; }
    public required string SystemPrompt { get; set; }
    public required string UserMessage { get; set; }
}

public class AgentAdviceDispatchRequest
{
    public required string CorrelationId { get; set; }
    public required string NuzlockeId { get; set; }
    public required string Question { get; set; }
    public required string Language { get; set; }
}

/// <summary>
/// Sent once immediately after the WebSocket connection is established.
/// Front should read this to confirm the nuzlocke context before sending requests.
/// </summary>
public class ConnectedMessage
{
    public string Type => "connected";
    public required string NuzlockeId { get; set; }
    /// <summary>True if the game state was auto-initialized during this connection.</summary>
    public bool Initialized { get; set; }
    public int Generation { get; set; }
    public string LockeType { get; set; } = "standard";
}
