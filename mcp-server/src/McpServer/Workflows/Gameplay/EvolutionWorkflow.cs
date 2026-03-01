using System.Text;
using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;

namespace es.vargontoc.nuzlocke.ai.Workflows.Gameplay;

/// <summary>
/// Workflow de evolución: actualiza especie, tipos y estadísticas de un pokemon
/// y genera un análisis LLM de las nuevas capacidades.
/// </summary>
public class EvolutionWorkflow : WorkflowBase
{
    private readonly INuzlockeFileManager _fileManager;
    private readonly IStatsCalculator _statsCalculator;

    private const string KeyPrevSpecies = "prev_species";
    private const string KeyPrevStats = "prev_stats";
    private const string KeyPrevTypes = "prev_types";
    private const string KeyNewData = "new_pokemon_data";

    public override string WorkflowId => "evolution";

    public EvolutionWorkflow(
        IStateManager stateManager,
        IPokeApiConnector pokeApi,
        IAiProvider aiProvider,
        INuzlockeFileManager fileManager,
        IStatsCalculator statsCalculator,
        ILogger<EvolutionWorkflow> logger)
        : base(stateManager, pokeApi, aiProvider, logger)
    {
        _fileManager = fileManager;
        _statsCalculator = statsCalculator;
    }

    public override IReadOnlyList<string> Validate(WorkflowParameters parameters)
    {
        var errors = ValidateRequired(parameters, "nuzlocke_id", "nickname", "evolved_species");
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

    protected override async Task FetchDataAsync(WorkflowContext context, CancellationToken ct)
    {
        var evolvedSpecies = NormalizeName(context.Parameters.GetString("evolved_species")!);
        var pokemonData = await PokeApi.GetPokemonSubsetAsync(evolvedSpecies);

        if (pokemonData == null)
            throw new InvalidOperationException(
                $"Species not found in PokeAPI: '{evolvedSpecies}'. Check spelling.");

        context.FetchedData[KeyNewData] = pokemonData;
    }

    protected override async Task MutateStateAsync(WorkflowContext context, CancellationToken ct)
    {
        var nickname = context.Parameters.GetString("nickname")!;
        var state = context.State;
        var newData = (PokemonSubset)context.FetchedData[KeyNewData];

        // Find pokemon — team first, then PC
        TeamMember? teamMember = state.Team.FirstOrDefault(p =>
            p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));
        StoredPokemon? pcMember = teamMember == null
            ? state.PCStorage.FirstOrDefault(p =>
                p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase))
            : null;

        if (teamMember == null && pcMember == null)
            throw new InvalidOperationException($"Pokemon '{nickname}' not found in team or PC.");

        // Snapshot before mutation for the LLM prompt
        var prevSpecies = teamMember?.Species ?? pcMember!.Species;
        var prevStats = teamMember?.Stats ?? pcMember!.Stats;
        var prevTypes = (teamMember?.Types ?? pcMember!.Types).ToList();
        var level = teamMember?.Level ?? pcMember!.Level;
        var dvs = teamMember?.DVs ?? pcMember!.DVs;
        var statExp = teamMember?.StatExp ?? pcMember!.StatExp;

        context.FetchedData[KeyPrevSpecies] = prevSpecies;
        context.FetchedData[KeyPrevStats] = prevStats;
        context.FetchedData[KeyPrevTypes] = prevTypes;

        // Recalculate stats with new base stats
        var newBaseStats = CapturePokemonWorkflow.ExtractBaseStats(newData.Stats);
        var newStats = _statsCalculator.Calculate(newBaseStats, dvs, statExp, level);
        var newTypes = newData.Types.ToList();
        var evolvedSpecies = newData.Name;

        // Apply mutations
        if (teamMember != null)
        {
            teamMember.Species = evolvedSpecies;
            teamMember.Types = newTypes;
            teamMember.Stats = newStats;
            teamMember.MaxHP = newStats.HP;
        }
        else
        {
            pcMember!.Species = evolvedSpecies;
            pcMember.Types = newTypes;
            pcMember.Stats = newStats;
        }

        await StateManager.SaveStateAsync(context.SessionId, state);
        context.State = state;

        context.Result.Mutations.Add(new StateMutation
        {
            Type = "evolution",
            Description = $"{nickname} evolved from {prevSpecies} to {evolvedSpecies}!"
        });

        context.Result.Data["nickname"] = nickname;
        context.Result.Data["prev_species"] = prevSpecies;
        context.Result.Data["new_species"] = evolvedSpecies;
        context.Result.Data["prev_types"] = prevTypes;
        context.Result.Data["new_types"] = newTypes;
        context.Result.Data["prev_stats"] = prevStats;
        context.Result.Data["new_stats"] = newStats;
        context.Result.Data["location"] = teamMember != null ? "team" : "pc";
    }

    protected override string GetSystemPrompt(WorkflowContext context) => $"""
        You are a Pokemon Nuzlocke advisor for Generation {context.State.Generation} ({context.State.LockeType} rules).
        Analyze a Pokemon evolution in the context of the current team.
        Focus on: stat improvements, type changes and their coverage implications, new role in the team.
        Keep the response under 250 words.
        You answer in {context.Language} language.
        """;

    protected override string BuildUserMessage(WorkflowContext context)
    {
        var nickname = context.Parameters.GetString("nickname")!;
        var trigger = context.Parameters.GetString("evolution_trigger") ?? "unknown";
        var prevSpecies = (string)context.FetchedData[KeyPrevSpecies];
        var prevStats = (PokemonStats)context.FetchedData[KeyPrevStats];
        var prevTypes = (List<string>)context.FetchedData[KeyPrevTypes];
        var newData = (PokemonSubset)context.FetchedData[KeyNewData];
        var newStats = (PokemonStats)context.Result.Data["new_stats"]!;

        var sb = new StringBuilder();
        sb.AppendLine($"{nickname} evolved from {prevSpecies} → {newData.Name} via {trigger}!");
        sb.AppendLine();

        sb.AppendLine($"TYPES: {string.Join(", ", prevTypes).PadRight(20)} → {string.Join(", ", newData.Types)}");
        sb.AppendLine();

        sb.AppendLine("STAT CHANGES:");
        sb.AppendLine($"  HP:      {prevStats.HP,4} → {newStats.HP,4}  ({StatDiff(prevStats.HP, newStats.HP)})");
        sb.AppendLine($"  Attack:  {prevStats.Attack,4} → {newStats.Attack,4}  ({StatDiff(prevStats.Attack, newStats.Attack)})");
        sb.AppendLine($"  Defense: {prevStats.Defense,4} → {newStats.Defense,4}  ({StatDiff(prevStats.Defense, newStats.Defense)})");
        sb.AppendLine($"  Speed:   {prevStats.Speed,4} → {newStats.Speed,4}  ({StatDiff(prevStats.Speed, newStats.Speed)})");
        sb.AppendLine($"  Special: {prevStats.Special,4} → {newStats.Special,4}  ({StatDiff(prevStats.Special, newStats.Special)})");
        sb.AppendLine();

        sb.AppendLine(BuildTeamSummary(context.State));
        sb.AppendLine();
        sb.AppendLine("How does this evolution change this Pokemon's role? Any new threats or opportunities?");

        return sb.ToString();
    }

    // ---- Helpers ----

    private static string NormalizeName(string name) =>
        name.Trim().ToLowerInvariant().Replace(' ', '-');

    private static string StatDiff(int prev, int now)
    {
        var delta = now - prev;
        return delta >= 0 ? $"+{delta}" : $"{delta}";
    }
}
