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
    public required string SessionId { get; set; }
    public required string WorkflowId { get; set; }
    public required string SystemPrompt { get; set; }
    public required string UserMessage { get; set; }
}

public class AgentAdviceDispatchRequest
{
    public required string CorrelationId { get; set; }
    public required string SessionId { get; set; }
    public required string Question { get; set; }
    public required string Language { get; set; }
}
