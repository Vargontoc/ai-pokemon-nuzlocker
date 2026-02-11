namespace es.vargontoc.nuzlocke.ai.Models;

/// <summary>
/// Response from AI provider that may include tool calls
/// </summary>
public class AiResponse
{
    /// <summary>
    /// Text response from the AI (may be empty if only tool calls)
    /// </summary>
    public string? TextResponse { get; set; }

    /// <summary>
    /// Tool calls requested by the AI
    /// </summary>
    public List<ToolCall> ToolCalls { get; set; } = new();

    /// <summary>
    /// Whether the AI wants to make tool calls
    /// </summary>
    public bool HasToolCalls => ToolCalls.Count > 0;
}

/// <summary>
/// A tool call requested by the AI
/// </summary>
public class ToolCall
{
    /// <summary>
    /// Unique ID for this tool call
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Name of the tool to call
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// JSON string of arguments for the tool
    /// </summary>
    public required string ArgumentsJson { get; set; }
}

/// <summary>
/// Result of executing a tool call
/// </summary>
public class ToolCallResult
{
    /// <summary>
    /// ID of the tool call this is a result for
    /// </summary>
    public required string ToolCallId { get; set; }

    /// <summary>
    /// Name of the tool that was called
    /// </summary>
    public required string ToolName { get; set; }

    /// <summary>
    /// Result content (usually JSON)
    /// </summary>
    public required string Content { get; set; }
}
