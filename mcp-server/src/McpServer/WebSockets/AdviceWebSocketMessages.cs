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

public class AdviceDispatchRequest
{
    public required string CorrelationId { get; set; }
    public required string SessionId { get; set; }
    public required string WorkflowId { get; set; }
    public required string SystemPrompt { get; set; }
    public required string UserMessage { get; set; }
}
