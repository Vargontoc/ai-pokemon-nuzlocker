namespace es.vargontoc.nuzlocke.ai.Workflows;

/// <summary>
/// Represents a structured workflow action that the web app can trigger.
/// Each workflow handles a specific game action (capture, battle, etc.).
/// </summary>
public interface IWorkflow
{
    /// <summary>
    /// Unique identifier for this workflow (e.g., "capture_pokemon", "start_battle")
    /// </summary>
    string WorkflowId { get; }

    /// <summary>
    /// Validates the incoming parameters before execution.
    /// Returns a list of validation errors (empty = valid).
    /// </summary>
    IReadOnlyList<string> Validate(WorkflowParameters parameters);

    /// <summary>
    /// Executes the workflow: deterministic state mutations first, then LLM analysis.
    /// </summary>
    Task<WorkflowResult> ExecuteAsync(WorkflowRequest request, CancellationToken ct = default);
}
