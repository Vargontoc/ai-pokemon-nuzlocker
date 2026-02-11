using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using es.vargontoc.nuzlocke.ai.Configuration;
using Microsoft.Extensions.Options;

namespace es.vargontoc.nuzlocke.ai.Services;

/// <summary>
/// AI Provider implementation for Anthropic Claude (cloud API)
/// </summary>
public class ClaudeAiProvider : IAiProvider
{
    private readonly AnthropicClient _client;
    private readonly AiOptions _options;
    private readonly ILogger<ClaudeAiProvider> _logger;

    public ClaudeAiProvider(
        IOptions<AiOptions> options,
        ILogger<ClaudeAiProvider> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "API Key is required for Claude provider. Configure AiProvider:ApiKey in appsettings.json");
        }

        _client = new AnthropicClient(new APIAuthentication(_options.ApiKey));
    }

    public async Task<string> GetCompletionAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = new List<Message>
            {
                new Message(RoleType.User, userMessage)
            };

            var parameters = new MessageParameters
            {
                Messages = messages,
                Model = _options.Model,
                System = new List<SystemMessage> { new SystemMessage(systemPrompt) },
                MaxTokens = _options.MaxTokens,
                Temperature = (decimal)_options.Temperature,
                Stream = false
            };

            _logger.LogDebug("Sending request to Claude API: model={Model}", _options.Model);

            var response = await _client.Messages.GetClaudeMessageAsync(parameters, cancellationToken);

            if (response?.Content == null || response.Content.Count == 0)
            {
                throw new InvalidOperationException("Claude returned empty response");
            }

            var textContent = response.Content
                .OfType<TextContent>()
                .Select(c => c.Text)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(textContent))
            {
                throw new InvalidOperationException("Claude response contained no text content");
            }

            _logger.LogInformation("Claude completion received: {Length} chars, tokens: {InputTokens}/{OutputTokens}",
                textContent.Length, response.Usage?.InputTokens, response.Usage?.OutputTokens);

            return textContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting completion from Claude API");
            throw;
        }
    }
}
