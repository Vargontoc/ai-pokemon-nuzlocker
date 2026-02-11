using es.vargontoc.nuzlocke.ai.Configuration;
using es.vargontoc.nuzlocke.ai.Models;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

namespace es.vargontoc.nuzlocke.ai.Providers.Impl;

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

    public async Task<AiResponse> GetCompletionWithToolsAsync(
        string systemPrompt,
        string userMessage,
        IEnumerable<ToolDefinition> tools,
        List<ToolCallResult>? toolResults = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userMessage)
            };

            // Add tool results from previous calls if any
            // OpenAI requires: Assistant message with tool_calls THEN tool messages with results
            if (toolResults != null && toolResults.Count > 0)
            {
                // Create assistant message with the tool calls that were executed
                var assistantToolCalls = toolResults.Select(result =>
                    ChatToolCall.CreateFunctionToolCall(
                        id: result.ToolCallId,
                        functionName: result.ToolName,
                        functionArguments: BinaryData.FromString("{}")  // Arguments not needed for reconstruction
                    )).ToList();

                messages.Add(new AssistantChatMessage(assistantToolCalls));

                // Now add the tool results
                foreach (var result in toolResults)
                {
                    messages.Add(new ToolChatMessage(result.ToolCallId, result.Content));
                }
            }

            // Convert tool definitions to OpenAI format
            var chatTools = tools.Select(tool => ChatTool.CreateFunctionTool(
                functionName: tool.Name,
                functionDescription: tool.Description,
                functionParameters: BinaryData.FromString(JsonSerializer.Serialize(new
                {
                    type = tool.Parameters.Type,
                    properties = tool.Parameters.Properties.ToDictionary(
                        p => p.Key,
                        p => new
                        {
                            type = p.Value.Type,
                            description = p.Value.Description
                        }),
                    required = tool.Parameters.Required
                }))
            )).ToList();

            var completionOptions = new ChatCompletionOptions
            {
                MaxOutputTokenCount = _options.MaxTokens,
                Temperature = (float)_options.Temperature
            };

            foreach (var tool in chatTools)
            {
                completionOptions.Tools.Add(tool);
            }

            _logger.LogDebug("Sending request to OpenAI with {ToolCount} tools", chatTools.Count);

            var response = await _client.CompleteChatAsync(messages, completionOptions, cancellationToken);

            var aiResponse = new AiResponse();

            if (response?.Value?.Content != null)
            {
                // Extract text content
                var textContent = string.Join("", response.Value.Content
                    .Select(c => c.Text)
                    .Where(t => !string.IsNullOrEmpty(t)));

                if (!string.IsNullOrEmpty(textContent))
                {
                    aiResponse.TextResponse = textContent;
                }
            }

            // Extract tool calls
            if (response?.Value?.ToolCalls != null)
            {
                foreach (var toolCall in response.Value.ToolCalls)
                {
                    aiResponse.ToolCalls.Add(new Models.ToolCall
                    {
                        Id = toolCall.Id,
                        Name = toolCall.FunctionName,
                        ArgumentsJson = toolCall.FunctionArguments.ToString()
                    });
                }
            }

            _logger.LogInformation("OpenAI completion received: text={HasText}, tools={ToolCount}, tokens: {InputTokens}/{OutputTokens}",
                aiResponse.TextResponse != null,
                aiResponse.ToolCalls.Count,
                response?.Value?.Usage?.InputTokenCount,
                response?.Value?.Usage?.OutputTokenCount);

            return aiResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting completion with tools from OpenAI API");
            throw;
        }
    }
}
