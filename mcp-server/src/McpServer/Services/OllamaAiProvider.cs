using System.Text;
using System.Text.Json;
using es.vargontoc.nuzlocke.ai.Configuration;
using Microsoft.Extensions.Options;

namespace es.vargontoc.nuzlocke.ai.Services;

/// <summary>
/// AI Provider implementation for Ollama (local LLM)
/// </summary>
public class OllamaAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;
    private readonly ILogger<OllamaAiProvider> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OllamaAiProvider(
        HttpClient httpClient,
        IOptions<AiOptions> options,
        ILogger<OllamaAiProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(120); // Ollama can be slow
    }

    public async Task<string> GetCompletionAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Combine system and user prompts for Ollama
            var combinedPrompt = $"{systemPrompt}\n\nUser: {userMessage}\n\nAssistant:";

            var request = new
            {
                model = _options.Model,
                prompt = combinedPrompt,
                stream = false,
                options = new
                {
                    temperature = _options.Temperature,
                    num_predict = _options.MaxTokens
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            _logger.LogDebug("Sending request to Ollama: {BaseUrl}/api/generate", _options.BaseUrl);

            var response = await _httpClient.PostAsync("/api/generate", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<OllamaResponse>(responseBody, _jsonOptions);

            if (result?.Response == null)
            {
                throw new InvalidOperationException("Ollama returned empty response");
            }

            _logger.LogInformation("Ollama completion received: {Length} chars", result.Response.Length);
            return result.Response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to Ollama at {BaseUrl}", _options.BaseUrl);
            throw new InvalidOperationException(
                $"Failed to connect to Ollama. Make sure Ollama is running at {_options.BaseUrl}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting completion from Ollama");
            throw;
        }
    }

    private class OllamaResponse
    {
        public string? Response { get; set; }
        public string? Model { get; set; }
        public bool Done { get; set; }
    }
}
