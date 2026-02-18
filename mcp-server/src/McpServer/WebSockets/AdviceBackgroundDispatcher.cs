using System.Text;
using es.vargontoc.nuzlocke.ai.Agents;
using es.vargontoc.nuzlocke.ai.Providers;

namespace es.vargontoc.nuzlocke.ai.WebSockets;

/// <summary>
/// Singleton dispatcher that spawns fire-and-forget background tasks
/// to stream LLM advice via WebSocket.
/// Uses IServiceScopeFactory to resolve scoped IAiProvider per task.
/// </summary>
public class AdviceBackgroundDispatcher : IAdviceDispatcher
{
    private readonly IAdviceConnectionManager _connectionManager;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AdviceBackgroundDispatcher> _logger;

    public AdviceBackgroundDispatcher(
        IAdviceConnectionManager connectionManager,
        IServiceScopeFactory scopeFactory,
        ILogger<AdviceBackgroundDispatcher> logger)
    {
        _connectionManager = connectionManager;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Dispatch(AdviceDispatchRequest request)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await ExecuteAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in advice dispatch for correlation {CorrelationId}",
                    request.CorrelationId);
            }
        });
    }

    public void DispatchAgentAdvice(AgentAdviceDispatchRequest request)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await ExecuteAgentAdviceAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in agent advice dispatch for correlation {CorrelationId}",
                    request.CorrelationId);
            }
        });
    }

    private async Task ExecuteAgentAdviceAsync(AgentAdviceDispatchRequest request)
    {
        if (!_connectionManager.HasConnection(request.SessionId))
        {
            _logger.LogWarning(
                "No WebSocket connection for session {SessionId}, skipping async agent advice for correlation {CorrelationId}",
                request.SessionId, request.CorrelationId);
            return;
        }
        
        var startSent = await _connectionManager.SendAsync(request.SessionId, new AdviceStartMessage
        {
            Type = "advice_start",
            CorrelationId = request.CorrelationId,
            WorkflowId = "agent_advice"
        });

        if (!startSent)
        {
            _logger.LogWarning("Failed to send advice_start for agent correlation {CorrelationId}", request.CorrelationId);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var agent = scope.ServiceProvider.GetRequiredService<NuzlockeAgent>();

            var advice = await agent.GetAdviceAsync(request.Question, CancellationToken.None, request.SessionId);

            await _connectionManager.SendAsync(request.SessionId, new AdviceEndMessage
            {
                Type = "advice_end",
                CorrelationId = request.CorrelationId,
                FullAdvice = advice
            });

            _logger.LogInformation(
                "Agent advice completed for correlation {CorrelationId} ({Length} chars)",
                request.CorrelationId, advice.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating agent advice for correlation {CorrelationId}", request.CorrelationId);

            await _connectionManager.SendAsync(request.SessionId, new AdviceErrorMessage
            {
                Type = "advice_error",
                CorrelationId = request.CorrelationId,
                Error = ex.Message
            });
        }
    }

    private async Task ExecuteAsync(AdviceDispatchRequest request)
    {
        // Check if there's a WebSocket connection for this session
        if (!_connectionManager.HasConnection(request.SessionId))
        {
            _logger.LogWarning(
                "No WebSocket connection for session {SessionId}, skipping async advice for correlation {CorrelationId}",
                request.SessionId, request.CorrelationId);
            return;
        }

        // Send advice_start
        var startSent = await _connectionManager.SendAsync(request.SessionId, new AdviceStartMessage
        {
            Type = "advice_start",
            CorrelationId = request.CorrelationId,
            WorkflowId = request.WorkflowId
        });

        if (!startSent)
        {
            _logger.LogWarning("Failed to send advice_start for correlation {CorrelationId}", request.CorrelationId);
            return;
        }

        try
        {
            // Create a scoped IAiProvider for this background task
            using var scope = _scopeFactory.CreateScope();
            var aiProvider = scope.ServiceProvider.GetRequiredService<IAiProvider>();

            var fullAdvice = new StringBuilder();

            await foreach (var chunk in aiProvider.StreamCompletionAsync(
                request.SystemPrompt, request.UserMessage))
            {
                fullAdvice.Append(chunk);

                var chunkSent = await _connectionManager.SendAsync(request.SessionId, new AdviceChunkMessage
                {
                    Type = "advice_chunk",
                    CorrelationId = request.CorrelationId,
                    Content = chunk
                });

                if (!chunkSent)
                {
                    _logger.LogWarning(
                        "WebSocket disconnected mid-stream for correlation {CorrelationId}, stopping",
                        request.CorrelationId);
                    return;
                }
            }

            // Send advice_end with the full advice
            await _connectionManager.SendAsync(request.SessionId, new AdviceEndMessage
            {
                Type = "advice_end",
                CorrelationId = request.CorrelationId,
                FullAdvice = fullAdvice.ToString()
            });

            _logger.LogInformation(
                "Advice streamed successfully for correlation {CorrelationId} ({Length} chars)",
                request.CorrelationId, fullAdvice.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating advice for correlation {CorrelationId}", request.CorrelationId);

            // Try to send error message via WebSocket
            await _connectionManager.SendAsync(request.SessionId, new AdviceErrorMessage
            {
                Type = "advice_error",
                CorrelationId = request.CorrelationId,
                Error = ex.Message
            });
        }
    }
}
