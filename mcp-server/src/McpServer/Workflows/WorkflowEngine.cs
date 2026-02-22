using System.Diagnostics;
using es.vargontoc.nuzlocke.ai.WebSockets;

namespace es.vargontoc.nuzlocke.ai.Workflows;

/// <summary>
/// Engine that dispatches workflow requests to the appropriate IWorkflow implementation.
/// </summary>
public interface IWorkflowEngine
{
    Task<WorkflowResult> ExecuteAsync(WorkflowRequest request, CancellationToken ct = default);

    /// <summary>
    /// Executes only the deterministic part and dispatches advice generation via WebSocket.
    /// Returns the HTTP-ready result immediately with a CorrelationId for the async advice.
    /// Falls back to sync ExecuteAsync if no WebSocket is connected for the session.
    /// </summary>
    Task<WorkflowResult> ExecuteWithAsyncAdviceAsync(WorkflowRequest request, CancellationToken ct = default);

    IReadOnlyList<string> GetAvailableWorkflows();
}

public class WorkflowEngine : IWorkflowEngine
{
    private readonly IReadOnlyDictionary<string, IWorkflow> _workflows;
    private readonly IAdviceConnectionManager _connectionManager;
    private readonly IAdviceDispatcher _adviceDispatcher;
    private readonly ILogger<WorkflowEngine> _logger;

    public WorkflowEngine(
        IEnumerable<IWorkflow> workflows,
        IAdviceConnectionManager connectionManager,
        IAdviceDispatcher adviceDispatcher,
        ILogger<WorkflowEngine> logger)
    {
        _logger = logger;
        _connectionManager = connectionManager;
        _adviceDispatcher = adviceDispatcher;
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

        await EmitWorkflowEventAsync(request.SessionId, result);

        return result;
    }

    public async Task<WorkflowResult> ExecuteWithAsyncAdviceAsync(WorkflowRequest request, CancellationToken ct = default)
    {
        if (!_workflows.TryGetValue(request.WorkflowId, out var workflow))
        {
            _logger.LogWarning("Unknown workflow requested: {WorkflowId}", request.WorkflowId);
            return WorkflowResult.Failure(request.WorkflowId,
                $"Unknown workflow: '{request.WorkflowId}'. Available: {string.Join(", ", _workflows.Keys)}");
        }

        // Determine the session ID for WebSocket lookup
        // For capture_pokemon-style workflows, the sessionId is the nuzlocke_id from parameters
        var sessionId = request.Parameters.GetString("nuzlocke_id") ?? request.SessionId;

        // If no WebSocket is connected, fall back to synchronous execution
        if (!_connectionManager.HasConnection(sessionId))
        {
            _logger.LogInformation(
                "No WebSocket for session {SessionId}, falling back to sync execution for {WorkflowId}",
                sessionId, request.WorkflowId);
            return await ExecuteAsync(request, ct);
        }

        _logger.LogInformation("Executing deterministic workflow {WorkflowId} with async advice for session {SessionId}",
            request.WorkflowId, sessionId);

        var sw = Stopwatch.StartNew();
        var deterministicResult = await workflow.ExecuteDeterministicAsync(request, ct);
        sw.Stop();

        _logger.LogInformation("Workflow {WorkflowId} deterministic part completed in {Ms:F0}ms. Success={Success}",
            request.WorkflowId, sw.Elapsed.TotalMilliseconds, deterministicResult.Result.Success);

        if (!deterministicResult.Result.Success)
        {
            await EmitWorkflowEventAsync(sessionId, deterministicResult.Result);
            return deterministicResult.Result;
        }

        // Generate correlationId and dispatch async advice
        if (!string.IsNullOrEmpty(deterministicResult.SystemPrompt) &&
            !string.IsNullOrEmpty(deterministicResult.UserMessage))
        {
            var correlationId = Guid.NewGuid().ToString("N");
            deterministicResult.Result.CorrelationId = correlationId;

            _adviceDispatcher.Dispatch(new AdviceDispatchRequest
            {
                CorrelationId = correlationId,
                SessionId = sessionId,
                WorkflowId = request.WorkflowId,
                SystemPrompt = deterministicResult.SystemPrompt,
                UserMessage = deterministicResult.UserMessage
            });

            _logger.LogInformation(
                "Advice dispatched for correlation {CorrelationId} on session {SessionId}",
                correlationId, sessionId);
        }

        await EmitWorkflowEventAsync(sessionId, deterministicResult.Result);

        return deterministicResult.Result;
    }

    private async Task EmitWorkflowEventAsync(string sessionId, WorkflowResult result)
    {
        if (!_connectionManager.HasConnection(sessionId))
            return;

        var sent = await _connectionManager.SendAsync(sessionId, new WorkflowEventMessage
        {
            Type = "workflow_event",
            CorrelationId = result.CorrelationId ?? Guid.NewGuid().ToString("N"),
            WorkflowId = result.WorkflowId,
            Success = result.Success,
            Mutations = result.Mutations,
            Data = result.Data,
            Errors = result.Errors
        });

        if (sent)
        {
            _logger.LogInformation(
                "Workflow event emitted for {WorkflowId} on session {SessionId} (success={Success}, mutations={MutationCount})",
                result.WorkflowId, sessionId, result.Success, result.Mutations.Count);
        }
    }

    public IReadOnlyList<string> GetAvailableWorkflows() =>
        _workflows.Keys.ToList().AsReadOnly();
}
