using System.Text;
using System.Text.Json;
using es.vargontoc.nuzlocke.ai.Configuration;
using es.vargontoc.nuzlocke.ai.Models;
using Microsoft.Extensions.Options;

namespace es.vargontoc.nuzlocke.ai.Providers.Impl;

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
        // Allow long-running Ollama calls (tools + large prompts can take time)
        _httpClient.Timeout = TimeSpan.FromMinutes(10); // 10 minutes
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

    public async Task<AiResponse> GetCompletionWithToolsAsync(
        string systemPrompt,
        string userMessage,
        IEnumerable<ToolDefinition> tools,
        List<ToolCallResult>? toolResults = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            };

            // Add tool results from previous calls if any
            if (toolResults != null)
            {
                foreach (var toolResult in toolResults)
                {
                    messages.Add(new
                    {
                        role = "tool",
                        content = toolResult.Content,
                        tool_call_id = toolResult.ToolCallId
                    });
                }
            }

            // Convert tool definitions to Ollama format
            var ollamaTools = tools.Select(tool => new
            {
                type = "function",
                function = new
                {
                    name = tool.Name,
                    description = tool.Description,
                    parameters = new
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
                }
            }).ToList();

            var request = new
            {
                model = _options.Model,
                messages,
                tools = ollamaTools,
                stream = false,
                options = new
                {
                    temperature = _options.Temperature,
                    num_predict = _options.MaxTokens,
                    num_ctx = 4096  // Context window for 10 tools + system prompt + game state
                }
            };

            var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(
                requestJson,
                Encoding.UTF8,
                "application/json");

            _logger.LogInformation("Sending request to Ollama: {ToolCount} tools ({ToolNames}), {Size} KB",
                ollamaTools.Count,
                string.Join(',', ollamaTools.Select(t => ((dynamic)t).function.name)),
                requestJson.Length / 1024.0);

            var response = await _httpClient.PostAsync("/api/chat", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Ollama returned {StatusCode}: {Error}",
                    response.StatusCode, errorBody);
            }

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            // TEMPORARY DEBUG LOGGING
            _logger.LogInformation("Ollama raw response: {Response}", responseBody);

            var result = JsonSerializer.Deserialize<OllamaChatResponse>(responseBody, _jsonOptions);

            // TEMPORARY DEBUG LOGGING
            _logger.LogInformation("Deserialized: Message={HasMessage}, Content={Content}, ToolCalls={ToolCalls}",
                result?.Message != null,
                result?.Message?.Content,
                result?.Message?.ToolCalls?.Count ?? 0);

            var aiResponse = new AiResponse();

            if (result?.Message != null)
            {
                // Extract text content
                if (!string.IsNullOrEmpty(result.Message.Content))
                {
                    aiResponse.TextResponse = result.Message.Content;
                }

                // Extract tool calls
                if (result.Message.ToolCalls != null)
                {
                    foreach (var toolCall in result.Message.ToolCalls)
                    {
                        if (toolCall.Function != null)
                        {
                            aiResponse.ToolCalls.Add(new Models.ToolCall
                            {
                                Id = toolCall.Id ?? Guid.NewGuid().ToString(),
                                Name = toolCall.Function.Name,
                                ArgumentsJson = JsonSerializer.Serialize(toolCall.Function.Arguments, _jsonOptions)
                            });
                        }
                    }
                }
            }

            _logger.LogInformation("Ollama completion received: text={HasText}, tools={ToolCount}",
                aiResponse.TextResponse != null,
                aiResponse.ToolCalls.Count);

            return aiResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to Ollama at {BaseUrl}", _options.BaseUrl);
            throw new InvalidOperationException(
                $"Failed to connect to Ollama. Make sure Ollama is running at {_options.BaseUrl}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting completion with tools from Ollama");
            throw;
        }
    }

    private class OllamaResponse
    {
        public string? Response { get; set; }
        public string? Model { get; set; }
        public bool Done { get; set; }
    }

    private class OllamaChatResponse
    {
        public OllamaMessage? Message { get; set; }
        public bool Done { get; set; }
    }

    private class OllamaMessage
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
        public List<OllamaToolCall>? ToolCalls { get; set; }
    }

    private class OllamaToolCall
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public OllamaFunction? Function { get; set; }
    }

    private class OllamaFunction
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object>? Arguments { get; set; }
    }
}
