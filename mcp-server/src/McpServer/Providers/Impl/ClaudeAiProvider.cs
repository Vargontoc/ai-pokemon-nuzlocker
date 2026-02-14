using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using es.vargontoc.nuzlocke.ai.Configuration;
using es.vargontoc.nuzlocke.ai.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;
using CommonTool = Anthropic.SDK.Common.Tool;

namespace es.vargontoc.nuzlocke.ai.Providers.Impl;

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

    public async Task<AiResponse> GetCompletionWithToolsAsync(
        string systemPrompt,
        string userMessage,
        IEnumerable<ToolDefinition> tools,
        List<ToolCallResult>? toolResults = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = new List<Message>
            {
                new Message(RoleType.User, userMessage)
            };

            // Add tool results from previous calls if any
            if (toolResults != null && toolResults.Count > 0)
            {
                // In Claude, tool results are added as user messages with tool_result content
                foreach (var toolCallResult in toolResults)
                {
                    var toolResultContent = new ToolResultContent
                    {
                        ToolUseId = toolCallResult.ToolCallId,
                        Content = new List<ContentBase>
                        {
                            new TextContent { Text = toolCallResult.Content }
                        }
                    };

                    messages.Add(new Message
                    {
                        Role = RoleType.User,
                        Content = new List<ContentBase> { toolResultContent }
                    });
                }
            }

            // Convert tool definitions to Claude format
            var claudeTools = tools.Select(tool =>
            {
                var toolJson = JsonSerializer.Serialize(new
                {
                    name = tool.Name,
                    description = tool.Description,
                    input_schema = new
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
                    }
                });
                return JsonSerializer.Deserialize<CommonTool>(toolJson);
            }).Where(t => t != null).Cast<CommonTool>().ToList();

            var parameters = new MessageParameters
            {
                Messages = messages,
                Model = _options.Model,
                System = new List<SystemMessage> { new SystemMessage(systemPrompt) },
                MaxTokens = _options.MaxTokens,
                Temperature = (decimal)_options.Temperature,
                Stream = false,
                Tools = claudeTools
            };

            _logger.LogDebug("Sending request to Claude API with {ToolCount} tools", claudeTools.Count);

            var response = await _client.Messages.GetClaudeMessageAsync(parameters, cancellationToken);

            var aiResponse = new AiResponse();

            if (response?.Content != null)
            {
                // Extract text content
                var textContent = string.Join("", response.Content
                    .OfType<TextContent>()
                    .Select(c => c.Text));

                if (!string.IsNullOrEmpty(textContent))
                {
                    aiResponse.TextResponse = textContent;
                }

                // Extract tool calls (tool_use blocks)
                var toolUseBlocks = response.Content.OfType<ToolUseContent>();
                foreach (var toolUse in toolUseBlocks)
                {
                    aiResponse.ToolCalls.Add(new Models.ToolCall
                    {
                        Id = toolUse.Id,
                        Name = toolUse.Name,
                        ArgumentsJson = JsonSerializer.Serialize(toolUse.Input)
                    });
                }
            }

            _logger.LogInformation("Claude completion received: text={HasText}, tools={ToolCount}, tokens: {InputTokens}/{OutputTokens}",
                aiResponse.TextResponse != null,
                aiResponse.ToolCalls.Count,
                response?.Usage?.InputTokens,
                response?.Usage?.OutputTokens);

            return aiResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting completion with tools from Claude API");
            throw;
        }
    }

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        string systemPrompt,
        string userMessage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var text = await GetCompletionAsync(systemPrompt, userMessage, cancellationToken);
        yield return text;
    }
}
