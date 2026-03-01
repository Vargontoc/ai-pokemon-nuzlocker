using es.vargontoc.nuzlocke.ai.Agents;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.WebSockets;
using Microsoft.AspNetCore.Mvc;

namespace es.vargontoc.nuzlocke.ai.Controllers;

/// <summary>
/// Nuzlocke AI advisor — ask questions and receive LLM-generated strategic advice.
/// </summary>
[ApiController]
[Route("nuzlocke")]
[Produces("application/json")]
public class NuzlockeController : ControllerBase
{
    private readonly ILogger<NuzlockeController> _logger;

    public NuzlockeController(ILogger<NuzlockeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Dispatch an async advice request. The LLM response is pushed via WebSocket.
    /// </summary>
    /// <remarks>
    /// Returns a `correlationId` immediately. Connect to `ws://host/ws/advice?sessionId=&lt;id&gt;`
    /// to receive the answer when processing completes.
    ///
    /// Example:
    ///
    ///     POST /nuzlocke/advice
    ///     {
    ///       "question": "Should I use Sparky against Misty?",
    ///       "sessionId": "my-session-id",
    ///       "language": "es-ES"
    ///     }
    /// </remarks>
    [HttpPost("advice")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAdvice(
        [FromBody] AdviceRequest request,
        [FromServices] IAdviceDispatcher dispatcher)
    {
        _logger.LogInformation("POST /nuzlocke/advice received: {Question}, session={SessionId}, language={Language}",
            request.Question, request.SessionId, request.Language);

        var sessionId = request.SessionId ?? "";
        var correlationId = Guid.NewGuid().ToString("N");
        var lng = request.Language ?? "en-EN";
        _logger.LogInformation(
            "Dispatching async agent advice for session {SessionId} with correlationId {CorrelationId}",
            sessionId, correlationId);

        dispatcher.DispatchAgentAdvice(new AgentAdviceDispatchRequest
        {
            CorrelationId = correlationId,
            SessionId = sessionId,
            Question = request.Question,
            Language = lng
        });

        return Task.FromResult<IActionResult>(
            Ok(new { question = request.Question, correlationId }));
    }

    /// <summary>
    /// Stream AI advice as Server-Sent Events (SSE).
    /// </summary>
    /// <remarks>
    /// Returns `Content-Type: text/event-stream`. Each chunk is sent as `data: {text}\n\n`.
    /// The stream ends with `event: end\ndata: [DONE]\n\n`.
    ///
    /// Example:
    ///
    ///     POST /nuzlocke/advice/stream
    ///     {
    ///       "question": "What's the best move against Brock?",
    ///       "sessionId": "my-session-id",
    ///       "language": "en-US"
    ///     }
    /// </remarks>
    [HttpPost("advice/stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task StreamAdvice(
        [FromBody] AdviceRequest request,
        [FromServices] NuzlockeAgent agent,
        CancellationToken ct)
    {
        _logger.LogInformation("POST /nuzlocke/advice/stream received: {Question}, session={SessionId}",
            request.Question, request.SessionId);

        Response.Headers["Cache-Control"] = "no-cache";
        Response.ContentType = "text/event-stream";

        try
        {
            await foreach (var chunk in agent.StreamAdviceAsync(request.Question, ct, request.SessionId))
            {
                if (ct.IsCancellationRequested) break;
                await Response.WriteAsync($"data: {chunk.Replace("\n", "\\n")}\n\n");
                await Response.Body.FlushAsync(ct);
            }

            await Response.WriteAsync("event: end\ndata: [DONE]\n\n");
            await Response.Body.FlushAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Client cancelled — no response needed
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while streaming advice");
        }
    }
}
