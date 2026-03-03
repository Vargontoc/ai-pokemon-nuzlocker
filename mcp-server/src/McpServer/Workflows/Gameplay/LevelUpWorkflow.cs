using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;

namespace es.vargontoc.nuzlocke.ai.Workflows.Gameplay;

/// <summary>
/// Mutation-only workflow: increments a Pokemon's level and recalculates Gen 1 stats.
/// No LLM advice is generated — the NuzlockeAgent produces the confirmation from the tool result.
/// </summary>
public class LevelUpWorkflow : WorkflowBase
{
    private readonly INuzlockeRepository _repository;
    private readonly IStatsCalculator _statsCalculator;
    private readonly IPokeApiConnector _pokeApi;

    public override string WorkflowId => "level_up";

    public LevelUpWorkflow(
        IStateManager stateManager,
        IPokeApiConnector pokeApi,
        IAiProvider aiProvider,
        INuzlockeRepository repository,
        IStatsCalculator statsCalculator,
        ILogger<LevelUpWorkflow> logger)
        : base(stateManager, pokeApi, aiProvider, logger)
    {
        _repository = repository;
        _statsCalculator = statsCalculator;
        _pokeApi = pokeApi;
    }

    public override IReadOnlyList<string> Validate(WorkflowParameters parameters)
    {
        var errors = ValidateRequired(parameters, "nuzlocke_id", "nickname");

        var newLevel = parameters.GetInt("new_level");
        if (newLevel.HasValue && newLevel.Value < 1)
            errors.Add("new_level must be >= 1");

        return errors;
    }

    protected override async Task<(WorkflowContext? Context, WorkflowResult? FailureResult)> BuildContextAsync(
        WorkflowRequest request, CancellationToken ct)
    {
        var errors = Validate(request.Parameters);
        if (errors.Count > 0)
            return (null, WorkflowResult.Failure(WorkflowId, errors.ToArray()));

        var nuzlockeId = request.Parameters.GetString("nuzlocke_id")!;
        var nuzlockePath = await _repository.GetNuzlockePathAsync(nuzlockeId);
        if (nuzlockePath == null)
        {
            return (null, WorkflowResult.Failure(WorkflowId,
                $"Nuzlocke not found: {nuzlockeId}. Ensure init_nuzlocke was called first."));
        }

        var state = await StateManager.GetStateAsync(nuzlockeId);
        var battleContext = await StateManager.GetBattleContextAsync(nuzlockeId);

        return (new WorkflowContext
        {
            NuzlockeId = nuzlockeId,
            Parameters = request.Parameters,
            State = state,
            BattleContext = battleContext,
            Result = new WorkflowResult { WorkflowId = WorkflowId, Success = true },
            Language = request.Language
        }, null);
    }

    protected override async Task MutateStateAsync(WorkflowContext context, CancellationToken ct)
    {
        var nickname = context.Parameters.GetString("nickname")!;
        var requestedLevel = context.Parameters.GetInt("new_level");
        var state = context.State;

        // Find pokemon in team first, then PC
        TeamMember? teamMember = state.Team.FirstOrDefault(p =>
            p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));

        StoredPokemon? pcMember = teamMember == null
            ? state.PCStorage.FirstOrDefault(p =>
                p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase))
            : null;

        if (teamMember == null && pcMember == null)
        {
            throw new InvalidOperationException(
                $"Pokemon '{nickname}' not found in team or PC.");
        }

        var currentLevel = teamMember?.Level ?? pcMember!.Level;
        var newLevel = requestedLevel ?? currentLevel + 1;

        if (newLevel <= currentLevel)
        {
            throw new InvalidOperationException(
                $"new_level ({newLevel}) must be greater than current level ({currentLevel}).");
        }

        // Fetch base stats from PokeAPI to recalculate
        var species = teamMember?.Species ?? pcMember!.Species;
        var dvs = teamMember?.DVs ?? pcMember!.DVs;
        var statExp = teamMember?.StatExp ?? pcMember!.StatExp;

        var pokemonData = await _pokeApi.GetPokemonSubsetAsync(species);
        var baseStats = pokemonData != null
            ? CapturePokemonWorkflow.ExtractBaseStats(pokemonData.Stats)
            : new[] { 45, 45, 45, 45, 45 };

        var newStats = _statsCalculator.Calculate(baseStats, dvs, statExp, newLevel);

        // Apply mutation
        if (teamMember != null)
        {
            teamMember.Level = newLevel;
            teamMember.Stats = newStats;
            teamMember.MaxHP = newStats.HP;
        }
        else
        {
            pcMember!.Level = newLevel;
            pcMember.Stats = newStats;
        }

        await StateManager.SaveStateAsync(context.NuzlockeId, state);
        context.State = state;

        context.Result.Mutations.Add(new StateMutation
        {
            Type = "level_up",
            Description = $"{nickname} ({species}) leveled up: {currentLevel} → {newLevel}"
        });

        context.Result.Data["nickname"] = nickname;
        context.Result.Data["species"] = species;
        context.Result.Data["previous_level"] = currentLevel;
        context.Result.Data["new_level"] = newLevel;
        context.Result.Data["stats"] = newStats;
        context.Result.Data["location"] = teamMember != null ? "team" : "pc";
    }

    // No LLM advice — returning empty strings skips GenerateAdviceAsync in WorkflowBase
    protected override string GetSystemPrompt(WorkflowContext context) => string.Empty;
    protected override string BuildUserMessage(WorkflowContext context) => string.Empty;
}
