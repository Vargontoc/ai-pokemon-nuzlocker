namespace es.vargontoc.nuzlocke.ai.Services;

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
}
