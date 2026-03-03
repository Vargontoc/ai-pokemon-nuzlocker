using es.vargontoc.nuzlocke.ai.Workflows;
using Microsoft.AspNetCore.Mvc;

namespace es.vargontoc.nuzlocke.ai.Controllers;

/// <summary>
/// Workflow execution engine — the core of all in-game event tracking.
/// </summary>
[ApiController]
[Route("nuzlocke")]
[Produces("application/json")]
public class WorkflowController : ControllerBase
{
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(ILogger<WorkflowController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Execute a gameplay workflow that mutates game state and optionally generates LLM advice.
    /// </summary>
    /// <remarks>
    /// The <c>parameters</c> field is a flexible key-value map. Required keys vary by workflow:
    ///
    /// | workflowId | Required params | Optional params |
    /// |---|---|---|
    /// | `init_nuzlocke` | `player_name`, `game_version`, `generation` | — |
    /// | `capture_pokemon` | `nuzlocke_id`, `species`, `nickname`, `location`, `level` | — |
    /// | `route_encounter` | `nuzlocke_id`, `route_name` | — |
    /// | `item_obtained` | `nuzlocke_id`, `item_name`, `quantity`, `category`, `location` | — |
    /// | `level_up` | `nuzlocke_id`, `nickname` | `new_level` (auto-increments if omitted) |
    /// | `manage_moves` (A) | `nuzlocke_id`, `nickname`, `moves` (array 1–4) | — |
    /// | `manage_moves` (B) | `nuzlocke_id`, `nickname`, `learn_move` | `forget_move`, `learn_source` |
    /// | `evolution` | `nuzlocke_id`, `nickname`, `evolved_species` | `evolution_trigger` |
    /// | `set_personality` | `nuzlocke_id`, `personality` | — |
    ///
    /// Example — capture a Pikachu:
    ///
    ///     POST /nuzlocke/workflow
    ///     {
    ///       "workflowId": "capture_pokemon",
    ///       "nuzlockeId": "my-session-id",
    ///       "language": "es-ES",
    ///       "parameters": {
    ///         "nuzlocke_id": "my-session-id",
    ///         "species": "pikachu",
    ///         "nickname": "Sparky",
    ///         "location": "Viridian Forest",
    ///         "level": 5
    ///       }
    ///     }
    ///
    /// Example — evolve Pikachu into Raichu:
    ///
    ///     POST /nuzlocke/workflow
    ///     {
    ///       "workflowId": "evolution",
    ///       "nuzlockeId": "my-session-id",
    ///       "parameters": {
    ///         "nuzlocke_id": "my-session-id",
    ///         "nickname": "Sparky",
    ///         "evolved_species": "Raichu",
    ///         "evolution_trigger": "stone"
    ///       }
    ///     }
    /// </remarks>
    [HttpPost("workflow")]
    [ProducesResponseType(typeof(WorkflowResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(WorkflowResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExecuteWorkflow(
        [FromBody] WorkflowRequest request,
        [FromServices] IWorkflowEngine engine,
        CancellationToken ct)
    {
        _logger.LogInformation("POST /nuzlocke/workflow: {WorkflowId}, nuzlocke={NuzlockeId}",
            request.WorkflowId, request.NuzlockeId);
        try
        {
            var result = await engine.ExecuteWithAsyncAdviceAsync(request, ct);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing workflow {WorkflowId}", request.WorkflowId);
            return Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// List all workflow IDs registered in the engine.
    /// </summary>
    [HttpGet("workflows")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAvailableWorkflows([FromServices] IWorkflowEngine engine)
    {
        return Ok(new { workflows = engine.GetAvailableWorkflows() });
    }
}
