using es.vargontoc.nuzlocke.ai.Agents;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.WebSockets;
using Microsoft.AspNetCore.Mvc;

namespace es.vargontoc.nuzlocke.ai.Controllers;

[ApiController]
[Route("nuzlocke")]
public class NuzlockeController : ControllerBase
{
    private readonly ILogger<NuzlockeController> _logger;

    public NuzlockeController(ILogger<NuzlockeController> logger)
    {
        _logger = logger;
    }

    [HttpPost("advice")]
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

    [HttpPost("advice/stream")]
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
