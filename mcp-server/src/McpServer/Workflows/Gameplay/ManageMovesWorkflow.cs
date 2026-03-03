using System.Text;
using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;

namespace es.vargontoc.nuzlocke.ai.Workflows.Gameplay;

/// <summary>
/// Workflow de gestión de movimientos. Dos vertientes detectadas automáticamente:
///
/// Vertiente A — Registro de movimientos actuales (sin LLM):
///   Params: nuzlocke_id, nickname, moves[]
///   Sobreescribe la lista de movimientos del pokemon.
///
/// Vertiente B — Aprender un movimiento nuevo (con análisis LLM):
///   Params: nuzlocke_id, nickname, learn_move, [forget_move], [learn_source]
///   Busca datos del movimiento en PokeAPI, aplica el cambio y genera consejo.
/// </summary>
public class ManageMovesWorkflow : WorkflowBase
{
    private readonly INuzlockeRepository _repository;

    private const string KeyVertiente = "vertiente";
    private const string KeyLearnMoveData = "learn_move_data";
    private const string KeyForgetMoveData = "forget_move_data";
    private const string KeyPokemon = "pokemon_ref";

    public override string WorkflowId => "manage_moves";

    public ManageMovesWorkflow(
        IStateManager stateManager,
        IPokeApiConnector pokeApi,
        IAiProvider aiProvider,
        INuzlockeRepository repository,
        ILogger<ManageMovesWorkflow> logger)
        : base(stateManager, pokeApi, aiProvider, logger)
    {
        _repository = repository;
    }

    public override IReadOnlyList<string> Validate(WorkflowParameters parameters)
    {
        var errors = ValidateRequired(parameters, "nuzlocke_id", "nickname");

        var isVertienteB = parameters.HasKey("learn_move");

        if (isVertienteB)
        {
            if (string.IsNullOrWhiteSpace(parameters.GetString("learn_move")))
                errors.Add("learn_move cannot be empty.");
        }
        else
        {
            var moves = parameters.GetStringArray("moves");
            if (moves == null || moves.Count == 0)
                errors.Add("Missing required parameter: moves (must provide at least 1 move).");
            else if (moves.Count > 4)
                errors.Add($"moves can have at most 4 entries, got {moves.Count}.");
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

    protected override async Task FetchDataAsync(WorkflowContext context, CancellationToken ct)
    {
        var isVertienteB = context.Parameters.HasKey("learn_move");
        context.FetchedData[KeyVertiente] = isVertienteB ? "B" : "A";

        if (!isVertienteB)
            return;

        var learnMoveName = NormalizeName(context.Parameters.GetString("learn_move")!);
        var learnMoveData = await PokeApi.GetMoveAsync(learnMoveName);
        if (learnMoveData == null)
            throw new InvalidOperationException($"Move not found in PokeAPI: '{learnMoveName}'. Check spelling.");

        context.FetchedData[KeyLearnMoveData] = learnMoveData;

        var forgetMoveName = context.Parameters.GetString("forget_move");
        if (!string.IsNullOrWhiteSpace(forgetMoveName))
        {
            var forgetMoveData = await PokeApi.GetMoveAsync(NormalizeName(forgetMoveName));
            if (forgetMoveData != null)
                context.FetchedData[KeyForgetMoveData] = forgetMoveData;
        }
    }

    protected override async Task MutateStateAsync(WorkflowContext context, CancellationToken ct)
    {
        var nickname = context.Parameters.GetString("nickname")!;
        var state = context.State;
        var vertiente = (string)context.FetchedData[KeyVertiente];

        // Find pokemon — team first, then PC
        TeamMember? teamMember = state.Team.FirstOrDefault(p =>
            p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));
        StoredPokemon? pcMember = teamMember == null
            ? state.PCStorage.FirstOrDefault(p =>
                p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase))
            : null;

        if (teamMember == null && pcMember == null)
            throw new InvalidOperationException($"Pokemon '{nickname}' not found in team or PC.");

        // Store reference for BuildUserMessage
        context.FetchedData[KeyPokemon] = (object?)teamMember ?? pcMember!;

        if (vertiente == "A")
        {
            var newMoves = context.Parameters.GetStringArray("moves")!;
            ApplyMoves(teamMember, pcMember, newMoves);

            context.Result.Mutations.Add(new StateMutation
            {
                Type = "moves_updated",
                Description = $"{nickname} moves updated: {string.Join(", ", newMoves)}"
            });
            context.Result.Data["moves"] = newMoves;
        }
        else
        {
            var learnMove = context.FetchedData[KeyLearnMoveData] is MoveData ld ? ld.Name : context.Parameters.GetString("learn_move")!;
            var forgetMove = context.Parameters.GetString("forget_move");
            var currentMoves = (teamMember?.Moves ?? pcMember!.Moves).ToList();

            if (!string.IsNullOrWhiteSpace(forgetMove))
            {
                var idx = currentMoves.FindIndex(m => m.Equals(NormalizeName(forgetMove), StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                    currentMoves[idx] = NormalizeName(learnMove);
                else
                    currentMoves.Add(NormalizeName(learnMove));
            }
            else if (currentMoves.Count < 4)
            {
                currentMoves.Add(NormalizeName(learnMove));
            }
            else
            {
                // Replace last move if full and no forget_move specified
                currentMoves[^1] = NormalizeName(learnMove);
            }

            ApplyMoves(teamMember, pcMember, currentMoves);

            var mutationDesc = string.IsNullOrWhiteSpace(forgetMove)
                ? $"{nickname} learned {learnMove}"
                : $"{nickname} forgot {forgetMove} and learned {learnMove}";

            context.Result.Mutations.Add(new StateMutation
            {
                Type = "move_learned",
                Description = mutationDesc
            });
            context.Result.Data["learned_move"] = learnMove;
            context.Result.Data["forgotten_move"] = forgetMove ?? string.Empty;
            context.Result.Data["moves"] = currentMoves;
        }

        await StateManager.SaveStateAsync(context.NuzlockeId, state);
        context.State = state;
    }

    protected override string GetSystemPrompt(WorkflowContext context)
    {
        if (!context.FetchedData.TryGetValue(KeyVertiente, out var v) || (string)v != "B")
            return string.Empty;

        return $"""
            You are a Pokemon Nuzlocke advisor for Generation {context.State.Generation} ({context.State.LockeType} rules).
            Analyze a newly learned move in the context of the Pokemon's current moveset and team.
            Focus on: move utility, type coverage improvement, synergy with the Pokemon's stats and role.
            Keep the response under 200 words.
            You answer in {context.Language} language.
            """;
    }

    protected override string BuildUserMessage(WorkflowContext context)
    {
        if (!context.FetchedData.TryGetValue(KeyVertiente, out var v) || (string)v != "B")
            return string.Empty;

        var nickname = context.Parameters.GetString("nickname")!;
        var learnSource = context.Parameters.GetString("learn_source") ?? "unknown";
        var learnMoveData = context.FetchedData.TryGetValue(KeyLearnMoveData, out var ld) ? ld as MoveData : null;
        var forgetMoveData = context.FetchedData.TryGetValue(KeyForgetMoveData, out var fd) ? fd as MoveData : null;

        var pokemon = context.FetchedData.TryGetValue(KeyPokemon, out var p) ? p : null;
        var species = pokemon is TeamMember tm ? tm.Species : pokemon is StoredPokemon sp ? sp.Species : "?";
        var currentMoves = (List<string>?)context.Result.Data.GetValueOrDefault("moves") ?? new List<string>();

        var sb = new StringBuilder();
        sb.AppendLine($"{nickname} ({species}) just learned a new move via {learnSource}.");
        sb.AppendLine();

        if (learnMoveData != null)
        {
            sb.AppendLine($"NEW MOVE: {learnMoveData.Name}");
            sb.AppendLine($"  Type: {learnMoveData.Type.Name} | Category: {learnMoveData.DamageClass.Name}");
            sb.AppendLine($"  Power: {learnMoveData.Power?.ToString() ?? "—"} | Accuracy: {learnMoveData.Accuracy?.ToString() ?? "—"}% | PP: {learnMoveData.Pp}");
            var effect = learnMoveData.EffectEntries.FirstOrDefault(e => e.Language.Name == "en")?.ShortEffect
                         ?? learnMoveData.EffectEntries.FirstOrDefault()?.ShortEffect ?? "No description.";
            sb.AppendLine($"  Effect: {effect}");
        }

        if (forgetMoveData != null)
        {
            sb.AppendLine();
            sb.AppendLine($"FORGOTTEN MOVE: {forgetMoveData.Name}");
            sb.AppendLine($"  Type: {forgetMoveData.Type.Name} | Category: {forgetMoveData.DamageClass.Name}");
            sb.AppendLine($"  Power: {forgetMoveData.Power?.ToString() ?? "—"} | Accuracy: {forgetMoveData.Accuracy?.ToString() ?? "—"}% | PP: {forgetMoveData.Pp}");
        }

        sb.AppendLine();
        sb.AppendLine($"CURRENT MOVESET: {string.Join(", ", currentMoves)}");
        sb.AppendLine();
        sb.AppendLine(BuildTeamSummary(context.State));
        sb.AppendLine();
        sb.AppendLine("Is this a good move to keep? Does it improve type coverage or team synergy?");

        return sb.ToString();
    }

    // ---- Helpers ----

    private static void ApplyMoves(TeamMember? team, StoredPokemon? pc, List<string> moves)
    {
        if (team != null) team.Moves = moves;
        else pc!.Moves = moves;
    }

    /// <summary>Normalizes move names: lowercase + spaces → hyphens.</summary>
    private static string NormalizeName(string name) =>
        name.Trim().ToLowerInvariant().Replace(' ', '-');
}
