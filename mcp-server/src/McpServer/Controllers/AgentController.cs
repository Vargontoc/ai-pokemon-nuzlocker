using es.vargontoc.nuzlocke.ai.Agents;
using es.vargontoc.nuzlocke.ai.Models;
using Microsoft.AspNetCore.Mvc;

namespace es.vargontoc.nuzlocke.ai.Controllers;

[ApiController]
[Route("agent")]
public class AgentController : ControllerBase
{
    private readonly ILogger<AgentController> _logger;

    public AgentController(ILogger<AgentController> logger)
    {
        _logger = logger;
    }

    [HttpPost("advice")]
    public async Task<IActionResult> GetAdvice(
        [FromBody] AdviceRequest request,
        [FromServices] PokeApiAgent agent,
        CancellationToken ct)
    {
        _logger.LogInformation("POST /agent/advice received: {Question}", request.Question);
        try
        {
            var advice = await agent.GetResponse(request.Question);
            return Ok(new { question = request.Question, advice });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling /agent/advice for question: {Question}", request.Question);
            return Problem(detail: ex.Message, statusCode: 500);
        }
    }
}
