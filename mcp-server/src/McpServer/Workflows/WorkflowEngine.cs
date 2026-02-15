using System.Diagnostics;

namespace es.vargontoc.nuzlocke.ai.Workflows;

/// <summary>
/// Engine that dispatches workflow requests to the appropriate IWorkflow implementation.
/// </summary>
public interface IWorkflowEngine
{
    Task<WorkflowResult> ExecuteAsync(WorkflowRequest request, CancellationToken ct = default);
    IReadOnlyList<string> GetAvailableWorkflows();
}

public class WorkflowEngine : IWorkflowEngine
{
    private readonly IReadOnlyDictionary<string, IWorkflow> _workflows;
    private readonly ILogger<WorkflowEngine> _logger;

    public WorkflowEngine(IEnumerable<IWorkflow> workflows, ILogger<WorkflowEngine> logger)
    {
        _logger = logger;
        _workflows = workflows.ToDictionary(w => w.WorkflowId, StringComparer.OrdinalIgnoreCase);
        _logger.LogInformation("WorkflowEngine initialized with {Count} workflows: {Ids}",
            _workflows.Count, string.Join(", ", _workflows.Keys));
    }

    public async Task<WorkflowResult> ExecuteAsync(WorkflowRequest request, CancellationToken ct = default)
    {
        if (!_workflows.TryGetValue(request.WorkflowId, out var workflow))
        {
            _logger.LogWarning("Unknown workflow requested: {WorkflowId}", request.WorkflowId);
            return WorkflowResult.Failure(request.WorkflowId,
                $"Unknown workflow: '{request.WorkflowId}'. Available: {string.Join(", ", _workflows.Keys)}");
        }

        _logger.LogInformation("Executing workflow {WorkflowId} for session {SessionId}",
            request.WorkflowId, request.SessionId);

        var sw = Stopwatch.StartNew();
        var result = await workflow.ExecuteAsync(request, ct);
        sw.Stop();

        _logger.LogInformation("Workflow {WorkflowId} completed in {Ms:F0}ms. Success={Success}",
            request.WorkflowId, sw.Elapsed.TotalMilliseconds, result.Success);

        return result;
    }

    public IReadOnlyList<string> GetAvailableWorkflows() =>
        _workflows.Keys.ToList().AsReadOnly();
}
