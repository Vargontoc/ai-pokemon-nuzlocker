using es.vargontoc.nuzlocke.ai.Workflows;
using Microsoft.AspNetCore.Mvc;

namespace es.vargontoc.nuzlocke.ai.Controllers;

[ApiController]
[Route("nuzlocke")]
public class WorkflowController : ControllerBase
{
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(ILogger<WorkflowController> logger)
    {
        _logger = logger;
    }

    [HttpPost("workflow")]
    public async Task<IActionResult> ExecuteWorkflow(
        [FromBody] WorkflowRequest request,
        [FromServices] IWorkflowEngine engine,
        CancellationToken ct)
    {
        _logger.LogInformation("POST /nuzlocke/workflow: {WorkflowId}, session={SessionId}",
            request.WorkflowId, request.SessionId);
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

    [HttpGet("workflows")]
    public IActionResult GetAvailableWorkflows([FromServices] IWorkflowEngine engine)
    {
        return Ok(new { workflows = engine.GetAvailableWorkflows() });
    }
}
