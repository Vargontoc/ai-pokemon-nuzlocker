using System.Text;
using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;

namespace es.vargontoc.nuzlocke.ai.Workflows.Gameplay;

/// <summary>
/// Workflow de consulta pre-captura.
/// Al llegar a una ruta nueva, el jugador indica qué pokemon están disponibles
/// y la IA aconseja cuál conviene capturar según equipo, PC y coberturas.
/// No muta estado — es puramente consultivo.
/// </summary>
public class RouteEncounterWorkflow : WorkflowBase
{
    private readonly INuzlockeRepository _repository;

    public override string WorkflowId => "route_encounter";

    public RouteEncounterWorkflow(
        IStateManager stateManager,
        IPokeApiConnector pokeApi,
        IAiProvider aiProvider,
        INuzlockeRepository repository,
        ILogger<RouteEncounterWorkflow> logger)
        : base(stateManager, pokeApi, aiProvider, logger)
    {
        _repository = repository;
    }

    public override IReadOnlyList<string> Validate(WorkflowParameters parameters)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(parameters.GetString("nuzlocke_id")))
            errors.Add("Missing required parameter: nuzlocke_id");

        if (string.IsNullOrWhiteSpace(parameters.GetString("route_name")))
            errors.Add("Missing required parameter: route_name");

        return errors;
    }

    /// <summary>
    /// Override: uses nuzlocke_id as sessionId, validates route has no prior encounter.
    /// </summary>
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
                $"Nuzlocke not found: {nuzlockeId}. Ensure init_nuzlocke was called and the server has discovered this nuzlocke."));
        }

        var state = await StateManager.GetStateAsync(nuzlockeId);
        var battleContext = await StateManager.GetBattleContextAsync(nuzlockeId);

        // Validate route has no prior encounter
        var routeName = request.Parameters.GetString("route_name")!;
        if (state.Encounters.ContainsKey(routeName))
        {
            return (null, WorkflowResult.Failure(WorkflowId,
                $"Route '{routeName}' already has a recorded encounter. Nuzlocke rule: one encounter per route."));
        }

        var context = new WorkflowContext
        {
            NuzlockeId = nuzlockeId,
            Parameters = request.Parameters,
            State = state,
            BattleContext = battleContext,
            Result = new WorkflowResult { WorkflowId = WorkflowId, Success = true },
            Language = request.Language
        };

        return (context, null);
    }

    protected override async Task FetchDataAsync(WorkflowContext context, CancellationToken ct)
    {
        var availablePokemon = context.Parameters.GetStringArray("available_pokemon") ?? new List<string>();
        var fetchedPokemon = new List<PokemonSubset>();

        // Fetch sequentially — DbContext (used by CachedPokeApiConnector) is not thread-safe
        foreach (var species in availablePokemon)
        {
            try
            {
                var pokemon = await PokeApi.GetPokemonSubsetAsync(species);
                if (pokemon != null)
                    fetchedPokemon.Add(pokemon);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to fetch data for pokemon {Species}, skipping", species);
            }
        }

        context.FetchedData["available_pokemon"] = fetchedPokemon;
        context.Result.Data["available_pokemon"] = fetchedPokemon;
        context.Result.Data["route_name"] = context.Parameters.GetString("route_name")!;
    }

    protected override Task MutateStateAsync(WorkflowContext context, CancellationToken ct)
    {
        // Consultation workflow — no state mutations
        return Task.CompletedTask;
    }

    protected override string GetSystemPrompt(WorkflowContext context)
    {
        return $"""
            You are a Pokemon Nuzlocke advisor for Generation {context.State.Generation} ({context.State.LockeType} rules).
            The player has arrived at a new route and wants to know which Pokemon to catch.
            Remember the Nuzlocke rule: only one capture per route, so the choice is critical.
            Analyze the available Pokemon against the current team and PC storage.
            Focus on: type coverage gaps, upcoming challenges, and survival priority.
            Keep the response under 300 words.
            You answer in {context.Language} language.
            """;
    }

    protected override string BuildUserMessage(WorkflowContext context)
    {
        var fetchedPokemon = (List<PokemonSubset>)context.FetchedData["available_pokemon"];
        var routeName = context.Parameters.GetString("route_name")!;

        var sb = new StringBuilder();
        sb.AppendLine($"I've arrived at {routeName}. Here are the Pokemon I can encounter:");
        sb.AppendLine();

        if (fetchedPokemon.Count == 0)
        {
            sb.AppendLine("No Pokemon data available for this route.");
        }
        else
        {
            foreach (var p in fetchedPokemon)
            {
                sb.AppendLine($"- {p.Name}: Types [{string.Join(", ", p.Types)}] | Stats: {string.Join(", ", p.Stats.Select(s => $"{s.Key}:{s.Value}"))} | Moves: {string.Join(", ", p.MovesBasicos)}");
            }
        }

        sb.AppendLine();
        sb.AppendLine(BuildTeamSummary(context.State));
        sb.AppendLine(BuildPCSummary(context.State));
        sb.AppendLine(BuildDeathsSummary(context.State));
        sb.AppendLine();
        sb.AppendLine("Which Pokemon should I try to catch? Why is it the best choice for my team?");
        sb.Append($"Respond in {context.Language} language");

        return sb.ToString();
    }

    private static string BuildPCSummary(NuzlockeState state)
    {
        if (state.PCStorage.Count == 0) return "PC: empty";
        var sb = new StringBuilder();
        sb.AppendLine($"PC Storage ({state.PCStorage.Count}):");
        foreach (var p in state.PCStorage)
        {
            var moves = p.Moves.Count > 0 ? string.Join(", ", p.Moves) : "none";
            sb.AppendLine($"  - {p.Nickname} ({p.Species} Lv.{p.Level}) Moves: {moves}");
        }
        return sb.ToString();
    }
}
