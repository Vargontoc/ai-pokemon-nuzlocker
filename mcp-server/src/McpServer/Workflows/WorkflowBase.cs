using System.Text;
using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;

namespace es.vargontoc.nuzlocke.ai.Workflows;

/// <summary>
/// Base class for all workflows. Implements the template method pattern:
/// Validate → FetchData → MutateState → GenerateAdvice
/// </summary>
public abstract class WorkflowBase : IWorkflow
{
    protected readonly IStateManager StateManager;
    protected readonly IPokeApiConnector PokeApi;
    protected readonly IAiProvider AiProvider;
    protected readonly ILogger Logger;

    public abstract string WorkflowId { get; }

    protected WorkflowBase(
        IStateManager stateManager,
        IPokeApiConnector pokeApi,
        IAiProvider aiProvider,
        ILogger logger)
    {
        StateManager = stateManager;
        PokeApi = pokeApi;
        AiProvider = aiProvider;
        Logger = logger;
    }

    public abstract IReadOnlyList<string> Validate(WorkflowParameters parameters);

    /// <summary>
    /// Builds the workflow context: validates, loads state, creates the context object.
    /// Returns null and sets the failure result on the out parameter when validation fails.
    /// Override in subclasses that need custom context building (e.g., skip state loading, resolve nuzlocke_id).
    /// </summary>
    protected virtual async Task<(WorkflowContext? Context, WorkflowResult? FailureResult)> BuildContextAsync(
        WorkflowRequest request, CancellationToken ct)
    {
        var errors = Validate(request.Parameters);
        if (errors.Count > 0)
            return (null, WorkflowResult.Failure(WorkflowId, errors.ToArray()));

        var state = await StateManager.GetStateAsync(request.NuzlockeId);
        var battleContext = await StateManager.GetBattleContextAsync(request.NuzlockeId);

        var context = new WorkflowContext
        {
            NuzlockeId = request.NuzlockeId,
            Parameters = request.Parameters,
            State = state,
            BattleContext = battleContext,
            Result = new WorkflowResult { WorkflowId = WorkflowId, Success = true },
            Language = request.Language
        };

        return (context, null);
    }

    public virtual async Task<WorkflowResult> ExecuteAsync(WorkflowRequest request, CancellationToken ct = default)
    {
        var (context, failure) = await BuildContextAsync(request, ct);
        if (context == null)
            return failure!;

        try
        {
            await FetchDataAsync(context, ct);
            await MutateStateAsync(context, ct);
            context.Result.Advice = await GenerateAdviceAsync(context, ct);
            return context.Result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Workflow {WorkflowId} failed for session {SessionId}",
                WorkflowId, request.NuzlockeId);
            return WorkflowResult.Failure(WorkflowId, $"Workflow execution failed: {ex.Message}");
        }
    }

    public virtual async Task<DeterministicResult> ExecuteDeterministicAsync(WorkflowRequest request, CancellationToken ct = default)
    {
        var (context, failure) = await BuildContextAsync(request, ct);
        if (context == null)
            return new DeterministicResult { Result = failure! };

        try
        {
            await FetchDataAsync(context, ct);
            await MutateStateAsync(context, ct);

            var systemPrompt = GetSystemPrompt(context);
            var userMessage = BuildUserMessage(context);

            return new DeterministicResult
            {
                Result = context.Result,
                SystemPrompt = systemPrompt,
                UserMessage = userMessage
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Workflow {WorkflowId} deterministic execution failed for session {SessionId}",
                WorkflowId, request.NuzlockeId);
            return new DeterministicResult
            {
                Result = WorkflowResult.Failure(WorkflowId, $"Workflow execution failed: {ex.Message}")
            };
        }
    }

    /// <summary>
    /// Override to fetch PokeAPI data before state mutations.
    /// </summary>
    protected virtual Task FetchDataAsync(WorkflowContext context, CancellationToken ct) =>
        Task.CompletedTask;

    /// <summary>
    /// Override to apply deterministic state mutations.
    /// </summary>
    protected virtual Task MutateStateAsync(WorkflowContext context, CancellationToken ct) =>
        Task.CompletedTask;

    /// <summary>
    /// Override to generate LLM advice. Base implementation calls IAiProvider.GetCompletionAsync.
    /// </summary>
    protected virtual async Task<string?> GenerateAdviceAsync(WorkflowContext context, CancellationToken ct)
    {
        var systemPrompt = GetSystemPrompt(context);
        var userMessage = BuildUserMessage(context);

        if (string.IsNullOrEmpty(systemPrompt) || string.IsNullOrEmpty(userMessage))
            return null;

        return await AiProvider.GetCompletionAsync(systemPrompt, userMessage, ct);
    }

    /// <summary>
    /// Short, focused system prompt for this specific workflow.
    /// </summary>
    protected abstract string GetSystemPrompt(WorkflowContext context);

    /// <summary>
    /// Build the user message containing state context + fetched data + parameters.
    /// </summary>
    protected abstract string BuildUserMessage(WorkflowContext context);

    // ---- Shared helpers ----

    protected string BuildTeamSummary(NuzlockeState state)
    {
        if (state.Team.Count == 0) return "Team: empty";
        var sb = new StringBuilder();
        sb.AppendLine($"Team ({state.Team.Count}/6):");
        foreach (var p in state.Team)
        {
            var moves = p.Moves.Count > 0 ? string.Join(", ", p.Moves) : "none";
            sb.AppendLine($"  - {p.Nickname} ({p.Species} Lv.{p.Level}) Moves: {moves}");
        }
        return sb.ToString();
    }

    protected string BuildDeathsSummary(NuzlockeState state)
    {
        if (state.DeadPokemon.Count == 0) return "Deaths: none";
        return $"Deaths ({state.DeadPokemon.Count}): " +
            string.Join(", ", state.DeadPokemon.Select(d => $"{d.Nickname} ({d.Species})"));
    }

    protected List<string> ValidateRequired(WorkflowParameters p, params string[] keys)
    {
        var validationErrors = new List<string>();
        foreach (var key in keys)
        {
            if (string.IsNullOrWhiteSpace(p.GetString(key)))
                validationErrors.Add($"Missing required parameter: {key}");
        }
        return validationErrors;
    }
}

/// <summary>
/// Mutable context passed through the workflow pipeline steps
/// </summary>
public class WorkflowContext
{
    public required string NuzlockeId { get; set; }
    public required WorkflowParameters Parameters { get; set; }
    public required NuzlockeState State { get; set; }
    public required BattleContext BattleContext { get; set; }
    public required WorkflowResult Result { get; set; }
    public required String Language { get; set; } = "en-US";
    /// <summary>
    /// Scratch space for passing fetched data between pipeline steps
    /// </summary>
    public Dictionary<string, object> FetchedData { get; set; } = new();
}
