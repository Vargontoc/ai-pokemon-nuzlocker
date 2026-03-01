using es.vargontoc.nuzlocke.ai.Agents;
using es.vargontoc.nuzlocke.ai.Models;
using Microsoft.AspNetCore.Mvc;

namespace es.vargontoc.nuzlocke.ai.Controllers;

/// <summary>
/// PokeAPI information agent — ask general Pokemon questions (types, moves, abilities, etc.).
/// </summary>
[ApiController]
[Route("agent")]
[Produces("application/json")]
public class AgentController : ControllerBase
{
    private readonly ILogger<AgentController> _logger;

    public AgentController(ILogger<AgentController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Ask the PokeAPI agent a general Pokemon question.
    /// </summary>
    /// <remarks>
    /// The agent has access to PokeAPI tools and can answer questions about moves,
    /// types, abilities, base stats, and more.
    ///
    /// Example:
    ///
    ///     POST /agent/advice
    ///     {
    ///       "question": "What are the weaknesses of a Water/Ice type?",
    ///       "language": "en-US"
    ///     }
    /// </remarks>
    [HttpPost("advice")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
