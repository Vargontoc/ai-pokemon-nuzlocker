using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;

namespace es.vargontoc.nuzlocke.ai.Workflows.Gameplay;

/// <summary>
/// Mutation-only workflow: changes the agent personality stored in NuzlockeState.
/// No LLM advice generated — the NuzlockeAgent confirms the change in its own response.
/// </summary>
public class SetPersonalityWorkflow : WorkflowBase
{
    private readonly INuzlockeFileManager _fileManager;

    public override string WorkflowId => "set_personality";

    public SetPersonalityWorkflow(
        IStateManager stateManager,
        IPokeApiConnector pokeApi,
        IAiProvider aiProvider,
        INuzlockeFileManager fileManager,
        ILogger<SetPersonalityWorkflow> logger)
        : base(stateManager, pokeApi, aiProvider, logger)
    {
        _fileManager = fileManager;
    }

    public override IReadOnlyList<string> Validate(WorkflowParameters parameters)
    {
        var errors = ValidateRequired(parameters, "nuzlocke_id", "personality");

        var raw = parameters.GetString("personality");
        if (!string.IsNullOrEmpty(raw) && !Enum.TryParse<AgentPersonality>(raw, ignoreCase: true, out _))
        {
            var valid = string.Join(", ", Enum.GetNames<AgentPersonality>());
            errors.Add($"Unknown personality '{raw}'. Valid values: {valid}");
        }

        return errors;
    }

    protected override async Task<(WorkflowContext? Context, WorkflowResult? FailureResult)> BuildContextAsync(
        WorkflowRequest request, CancellationToken ct)
    {
        var errors = Validate(request.Parameters);
        if (errors.Count > 0)
            return (null, WorkflowResult.Failure(WorkflowId, errors.ToArray()));

        var nuzlockeId = request.Parameters.GetString("nuzlocke_id")!;
        var nuzlockePath = await _fileManager.GetNuzlockePathAsync(nuzlockeId);
        if (nuzlockePath == null)
        {
            return (null, WorkflowResult.Failure(WorkflowId,
                $"Nuzlocke not found: {nuzlockeId}. Ensure init_nuzlocke was called first."));
        }

        var state = await StateManager.GetStateAsync(nuzlockeId);
        var battleContext = await StateManager.GetBattleContextAsync(nuzlockeId);

        return (new WorkflowContext
        {
            SessionId = nuzlockeId,
            Parameters = request.Parameters,
            State = state,
            BattleContext = battleContext,
            Result = new WorkflowResult { WorkflowId = WorkflowId, Success = true },
            Language = request.Language
        }, null);
    }

    protected override async Task MutateStateAsync(WorkflowContext context, CancellationToken ct)
    {
        var raw = context.Parameters.GetString("personality")!;
        var newPersonality = Enum.Parse<AgentPersonality>(raw, ignoreCase: true);
        var previous = context.State.Personality;

        context.State.Personality = newPersonality;
        await StateManager.SaveStateAsync(context.SessionId, context.State);

        context.Result.Mutations.Add(new StateMutation
        {
            Type = "personality_changed",
            Description = $"Agent personality changed: {previous} → {newPersonality}"
        });

        context.Result.Data["previous_personality"] = previous.ToString();
        context.Result.Data["new_personality"] = newPersonality.ToString();
    }

    protected override string GetSystemPrompt(WorkflowContext context) => string.Empty;
    protected override string BuildUserMessage(WorkflowContext context) => string.Empty;
}
