namespace es.vargontoc.nuzlocke.ai.Configuration;

public class AiOptions
{
    public const string SectionName = "AiProvider";

    /// <summary>
    /// Provider type: "Claude" or "Ollama"
    /// </summary>
    public string Provider { get; set; } = "Ollama";

    /// <summary>
    /// API Key for cloud providers (Claude)
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Base URL for the AI provider
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Model name to use
    /// </summary>
    public string Model { get; set; } = "llama3.2";

    /// <summary>
    /// Maximum tokens for completion
    /// </summary>
    public int MaxTokens { get; set; } = 1024;

    /// <summary>
    /// Temperature for response generation (0.0 - 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.7;
}
