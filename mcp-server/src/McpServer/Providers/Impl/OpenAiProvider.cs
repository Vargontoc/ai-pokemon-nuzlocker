using es.vargontoc.nuzlocke.ai.Configuration;
using es.vargontoc.nuzlocke.ai.Models;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
    private readonly HttpClient _httpClient;

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
        _httpClient = new HttpClient();
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

            var response = await RetryHelper.ExecuteWithRetriesAsync(
                () => _client.CompleteChatAsync(messages, completionOptions, cancellationToken),
                _options.MaxRetries,
                _logger,
                cancellationToken);

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

            var response = await RetryHelper.ExecuteWithRetriesAsync(
                () => _client.CompleteChatAsync(messages, completionOptions, cancellationToken),
                _options.MaxRetries,
                _logger,
                cancellationToken);

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

    // Single-attempt streaming implementation (parses SSE lines)
    private async IAsyncEnumerable<string> StreamOnceAsync(
        string systemPrompt,
        string userMessage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model = _options.Model,
            messages = new[] {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            max_tokens = _options.MaxTokens,
            temperature = _options.Temperature,
            stream = true
        };

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var functionCallBuffer = new StringBuilder();
        var inFunctionCall = false;

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // OpenAI SSE streams lines prefixed with "data: "
            if (line.StartsWith("data: "))
            {
                var data = line.Substring("data: ".Length).Trim();
                if (data == "[DONE]")
                {
                    if (inFunctionCall && functionCallBuffer.Length > 0)
                    {
                        yield return functionCallBuffer.ToString();
                        functionCallBuffer.Clear();
                        inFunctionCall = false;
                    }
                    yield break;
                }

                if (data.StartsWith("{"))
                {
                    var doc = JsonDocument.Parse(data);
                    var root = doc.RootElement;
                    var choices = root.GetProperty("choices");
                    if (choices.GetArrayLength() > 0)
                    {
                        var delta = choices[0].GetProperty("delta");
                        if (delta.TryGetProperty("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.String)
                        {
                            var chunk = contentEl.GetString();
                            if (!string.IsNullOrEmpty(chunk))
                            {
                                if (inFunctionCall)
                                {
                                    functionCallBuffer.Append(chunk);
                                }
                                else
                                {
                                    yield return chunk;
                                }
                            }
                        }
                        else if (delta.TryGetProperty("function_call", out var fc))
                        {
                            // function_call may arrive in fragments: accumulate
                            inFunctionCall = true;
                            functionCallBuffer.Append(fc.ToString());
                        }
                    }
                    else
                    {
                        yield return line;
                    }
                }
                else
                {
                    yield return line;
                }
            }
            else
            {
                // Non-prefixed line, yield raw
                yield return line;
            }
        }
    }

    public IAsyncEnumerable<string> StreamCompletionAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        // Wrap single-attempt stream with retries using a channel
        return StreamWithRetriesAsync(systemPrompt, userMessage, StreamOnceAsync, cancellationToken);
    }

    private IAsyncEnumerable<string> StreamWithRetriesAsync(
        string systemPrompt,
        string userMessage,
        Func<string, string, CancellationToken, IAsyncEnumerable<string>> streamFunc,
        CancellationToken cancellationToken = default)
    {
        var channel = System.Threading.Channels.Channel.CreateUnbounded<string>();

        _ = Task.Run(async () =>
        {
            var maxAttempts = 3;
            var attempt = 0;
            var rnd = new Random();
            Exception? lastEx = null;

            while (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
                attempt++;
                try
                {
                    await foreach (var part in streamFunc(systemPrompt, userMessage, cancellationToken))
                    {
                        await channel.Writer.WriteAsync(part, cancellationToken);
                    }
                    channel.Writer.Complete();
                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    channel.Writer.TryComplete(new OperationCanceledException());
                    return;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    _logger.LogWarning(ex, "Streaming attempt {Attempt} failed", attempt);
                    if (attempt >= maxAttempts)
                    {
                        channel.Writer.TryComplete(ex);
                        return;
                    }
                    var delayMs = (int)(Math.Pow(2, attempt) * 100) + rnd.Next(0, 200);
                    await Task.Delay(delayMs, cancellationToken);
                }
            }

            if (lastEx != null)
            {
                channel.Writer.TryComplete(lastEx);
            }
            else
            {
                channel.Writer.TryComplete();
            }
        }, cancellationToken);

        return channel.Reader.ReadAllAsync(cancellationToken);
    }
}
