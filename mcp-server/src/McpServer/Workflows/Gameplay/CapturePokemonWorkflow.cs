using System.Text;
using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;

namespace es.vargontoc.nuzlocke.ai.Workflows.Gameplay;

/// <summary>
/// Workflow de captura de pokemon.
/// Registra el encuentro (regla 1 captura por ruta), añade al equipo o PC,
/// y genera consejo estratégico analizando el capturado vs equipo actual.
/// </summary>
public class CapturePokemonWorkflow : WorkflowBase
{
    private readonly INuzlockeFileManager _fileManager;
    private readonly IStatsCalculator _statsCalculator;

    public override string WorkflowId => "capture_pokemon";

    public CapturePokemonWorkflow(
        IStateManager stateManager,
        IPokeApiConnector pokeApi,
        IAiProvider aiProvider,
        INuzlockeFileManager fileManager,
        IStatsCalculator statsCalculator,
        ILogger<CapturePokemonWorkflow> logger)
        : base(stateManager, pokeApi, aiProvider, logger)
    {
        _fileManager = fileManager;
        _statsCalculator = statsCalculator;
    }

    public override IReadOnlyList<string> Validate(WorkflowParameters parameters)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(parameters.GetString("nuzlocke_id")))
            errors.Add("Missing required parameter: nuzlocke_id");

        if (string.IsNullOrWhiteSpace(parameters.GetString("species")))
            errors.Add("Missing required parameter: species");

        if (string.IsNullOrWhiteSpace(parameters.GetString("nickname")))
            errors.Add("Missing required parameter: nickname");

        if (string.IsNullOrWhiteSpace(parameters.GetString("location")))
            errors.Add("Missing required parameter: location");

        if (parameters.GetInt("level") == null)
            errors.Add("Missing required parameter: level");

        return errors;
    }

    /// <summary>
    /// Override: uses nuzlocke_id as sessionId to load state from NuzlockeFileManager.
    /// Also ensures the nuzlocke_id is resolved in the file manager's path cache.
    /// </summary>
    protected override async Task<(WorkflowContext? Context, WorkflowResult? FailureResult)> BuildContextAsync(
        WorkflowRequest request, CancellationToken ct)
    {
        var errors = Validate(request.Parameters);
        if (errors.Count > 0)
            return (null, WorkflowResult.Failure(WorkflowId, errors.ToArray()));

        var nuzlockeId = request.Parameters.GetString("nuzlocke_id")!;

        // Ensure the nuzlocke_id is known (L1 memory cache → L2 SQLite registry)
        var nuzlockePath = await _fileManager.GetNuzlockePathAsync(nuzlockeId);
        if (nuzlockePath == null)
        {
            return (null, WorkflowResult.Failure(WorkflowId,
                $"Nuzlocke not found: {nuzlockeId}. Ensure init_nuzlocke was called and the server has discovered this nuzlocke."));
        }

        // Use nuzlocke_id as the sessionId so StateManager delegates to NuzlockeFileManager
        var state = await StateManager.GetStateAsync(nuzlockeId);
        var battleContext = await StateManager.GetBattleContextAsync(nuzlockeId);

        var context = new WorkflowContext
        {
            SessionId = nuzlockeId,
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
        var species = context.Parameters.GetString("species")!;
        var pokemonData = await PokeApi.GetPokemonSubsetAsync(species);

        if (pokemonData == null)
        {
            throw new InvalidOperationException($"Pokemon not found: {species}");
        }

        context.FetchedData["pokemon"] = pokemonData;
        context.Result.Data["pokemon"] = pokemonData;
    }

    protected override async Task MutateStateAsync(WorkflowContext context, CancellationToken ct)
    {
        var species = context.Parameters.GetString("species")!;
        var nickname = context.Parameters.GetString("nickname")!;
        var location = context.Parameters.GetString("location")!;
        var level = context.Parameters.GetInt("level") ?? 1;
        var pokemonData = (PokemonSubset)context.FetchedData["pokemon"];

        // 1. Record encounter (enforces 1-capture-per-route rule)
        var encounterRecorded = await StateManager.RecordEncounterAsync(
            context.SessionId, location, species, nickname);

        if (!encounterRecorded)
        {
            throw new InvalidOperationException($"Encounter already recorded for location: {location}");
        }

        context.Result.Mutations.Add(new StateMutation
        {
            Type = "encounter_recorded",
            Description = $"Recorded encounter at {location}: {nickname} ({species})"
        });

        // 2. Add to team or PC based on team size
        // Re-read state after encounter was recorded
        var state = await StateManager.GetStateAsync(context.SessionId);

        var baseStats = ExtractBaseStats(pokemonData.Stats);
        var defaultDvs = new[] { 8, 8, 8, 8, 8 };
        var defaultStatExp = new[] { 0, 0, 0, 0, 0 };
        var calculatedStats = _statsCalculator.Calculate(baseStats, defaultDvs, defaultStatExp, level);

        var teamMember = new TeamMember
        {
            Nickname = nickname,
            Species = species,
            Level = level,
            Moves = pokemonData.MovesBasicos.Take(4).ToList(),
            CaughtAt = location,
            CaughtDate = DateTime.UtcNow,
            DVs = defaultDvs,
            StatExp = defaultStatExp,
            Stats = calculatedStats
        };

        string destination;

        if (state.Team.Count < 6)
        {
            var added = await StateManager.AddToTeamAsync(context.SessionId, teamMember);
            if (!added)
                throw new InvalidOperationException($"Failed to add {nickname} to team");

            destination = "team";
            context.Result.Mutations.Add(new StateMutation
            {
                Type = "added_to_team",
                Description = $"{nickname} ({species} Lv.{level}) added to team"
            });
        }
        else
        {
            // Team full — add directly to PC storage
            var storedPokemon = new StoredPokemon
            {
                Nickname = nickname,
                Species = species,
                Level = level,
                Moves = pokemonData.MovesBasicos.Take(4).ToList(),
                CaughtAt = location,
                CaughtDate = DateTime.UtcNow,
                DVs = defaultDvs,
                StatExp = defaultStatExp,
                Stats = calculatedStats
            };

            state.PCStorage.Add(storedPokemon);
            await StateManager.SaveStateAsync(context.SessionId, state);

            destination = "pc";
            context.Result.Mutations.Add(new StateMutation
            {
                Type = "added_to_pc",
                Description = $"{nickname} ({species} Lv.{level}) sent to PC (team full)"
            });
        }

        context.Result.Data["destination"] = destination;

        // Update context state for advice generation
        context.State = await StateManager.GetStateAsync(context.SessionId);
    }

    protected override string GetSystemPrompt(WorkflowContext context)
    {
        return $"""
            You are a Pokemon Nuzlocke advisor for Generation {context.State.Generation} ({context.State.LockeType} rules).
            Analyze a newly captured Pokemon in the context of the current team.
            Focus on: type coverage, strengths/weaknesses, and whether this Pokemon fills a gap in the team.
            Keep the response under 250 words.
            You answer in {context.Language} language.
            """;
    }

    /// <summary>
    /// Maps PokeAPI stat dict → int[5] in Gen 1 order: [HP, Atk, Def, Spe, Sp].
    /// Uses "special-attack" as the single Gen 1 special stat.
    /// </summary>
    internal static int[] ExtractBaseStats(Dictionary<string, int> stats)
    {
        return new[]
        {
            stats.GetValueOrDefault("hp", 45),
            stats.GetValueOrDefault("attack", 45),
            stats.GetValueOrDefault("defense", 45),
            stats.GetValueOrDefault("speed", 45),
            stats.GetValueOrDefault("special-attack", 45)
        };
    }

    protected override string BuildUserMessage(WorkflowContext context)
    {
        var pokemonData = (PokemonSubset)context.FetchedData["pokemon"];
        var destination = (string)context.Result.Data["destination"]!;
        var nickname = context.Parameters.GetString("nickname")!;

        var sb = new StringBuilder();
        sb.AppendLine($"I just captured {nickname} ({pokemonData.Name}) at {context.Parameters.GetString("location")}.");
        sb.AppendLine($"Types: {string.Join(", ", pokemonData.Types)}");
        sb.AppendLine($"Stats: {string.Join(", ", pokemonData.Stats.Select(s => $"{s.Key}: {s.Value}"))}");
        sb.AppendLine($"Moves: {string.Join(", ", pokemonData.MovesBasicos)}");
        sb.AppendLine($"Destination: {destination}");
        sb.AppendLine();
        sb.AppendLine(BuildTeamSummary(context.State));
        sb.AppendLine(BuildDeathsSummary(context.State));
        sb.AppendLine();
        sb.AppendLine("How does this capture help my team? Should I swap anyone out?");
        sb.Append($"Respond me {context.Language} language");
        Logger.LogInformation(sb.ToString());
        return sb.ToString();
    }
}
