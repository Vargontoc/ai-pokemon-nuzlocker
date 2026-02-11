using es.vargontoc.nuzlocke.ai.Configuration;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace es.vargontoc.nuzlocke.ai.Services;

/// <summary>
/// AI Provider implementation for OpenAI (GPT models)
/// </summary>
public class OpenAiProvider : IAiProvider
{
    private readonly ChatClient _client;
    private readonly AiOptions _options;
    private readonly ILogger<OpenAiProvider> _logger;

    public OpenAiProvider(
        IOptions<AiOptions> options,
        ILogger<OpenAiProvider> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "API Key is required for OpenAI provider. Configure AiProvider:ApiKey in appsettings.json");
        }

        _client = new ChatClient(_options.Model, _options.ApiKey);
    }

    public async Task<string> GetCompletionAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userMessage)
            };

            var completionOptions = new ChatCompletionOptions
            {
                MaxOutputTokenCount = _options.MaxTokens,
                Temperature = (float)_options.Temperature
            };

            _logger.LogDebug("Sending request to OpenAI: model={Model}", _options.Model);

            var response = await _client.CompleteChatAsync(messages, completionOptions, cancellationToken);

            if (response?.Value?.Content == null || response.Value.Content.Count == 0)
            {
                throw new InvalidOperationException("OpenAI returned empty response");
            }

            var textContent = string.Join("", response.Value.Content
                .Select(c => c.Text)
                .Where(t => !string.IsNullOrEmpty(t)));

            if (string.IsNullOrEmpty(textContent))
            {
                throw new InvalidOperationException("OpenAI response contained no text content");
            }

            _logger.LogInformation("OpenAI completion received: {Length} chars, tokens: {InputTokens}/{OutputTokens}",
                textContent.Length,
                response.Value.Usage?.InputTokenCount,
                response.Value.Usage?.OutputTokenCount);

            return textContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting completion from OpenAI API");
            throw;
        }
    }
}
