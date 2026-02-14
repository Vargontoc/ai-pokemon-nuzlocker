using es.vargontoc.nuzlocke.ai.Models;

namespace es.vargontoc.nuzlocke.ai.Providers;

/// <summary>
/// Interface for AI provider implementations (Claude, Ollama, etc.)
/// </summary>
public interface IAiProvider
{
    /// <summary>
    /// Get completion from AI provider
    /// </summary>
    /// <param name="systemPrompt">System instructions for the AI</param>
    /// <param name="userMessage">User's question or message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI response text</returns>
    Task<string> GetCompletionAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get completion with function calling support
    /// </summary>
    /// <param name="systemPrompt">System instructions for the AI</param>
    /// <param name="userMessage">User's question or message</param>
    /// <param name="tools">Available tools the AI can call</param>
    /// <param name="toolResults">Results from previous tool calls (for multi-turn)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI response with possible tool calls</returns>
    Task<AiResponse> GetCompletionWithToolsAsync(
        string systemPrompt,
        string userMessage,
        IEnumerable<ToolDefinition> tools,
        List<ToolCallResult>? toolResults = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream completion chunks from the AI provider as they arrive.
    /// </summary>
    IAsyncEnumerable<string> StreamCompletionAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default);
}
