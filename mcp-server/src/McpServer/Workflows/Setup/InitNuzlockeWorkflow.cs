using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;

namespace es.vargontoc.nuzlocke.ai.Workflows.Setup;

/// <summary>
/// One-shot workflow para inicializar una partida Nuzlocke.
/// Crea la estructura de carpetas, metadata y game_state inicial.
/// Input: generation (int, solo 1), locke_type (string), base_path (string, obligatorio)
/// </summary>
public class InitNuzlockeWorkflow : WorkflowBase
{
    private readonly INuzlockeFileManager _fileManager;

    public override string WorkflowId => "init_nuzlocke";

    public InitNuzlockeWorkflow(
        IStateManager stateManager,
        IPokeApiConnector pokeApi,
        IAiProvider aiProvider,
        INuzlockeFileManager fileManager,
        ILogger<InitNuzlockeWorkflow> logger)
        : base(stateManager, pokeApi, aiProvider, logger)
    {
        _fileManager = fileManager;
    }

    public override IReadOnlyList<string> Validate(WorkflowParameters parameters)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(parameters.GetString("base_path")))
            errors.Add("Missing required parameter: base_path");

        var generation = parameters.GetInt("generation");
        if (generation == null)
            errors.Add("Missing required parameter: generation");
        else if (generation != 1)
            errors.Add("Only generation 1 is supported");

        // locke_type is optional, defaults to "standard"

        return errors;
    }

    /// <summary>
    /// Override: init_nuzlocke is a creation workflow — no pre-existing state to load.
    /// Skips StateManager.GetStateAsync to avoid "session not found" errors.
    /// </summary>
    public override async Task<WorkflowResult> ExecuteAsync(WorkflowRequest request, CancellationToken ct = default)
    {
        var errors = Validate(request.Parameters);
        if (errors.Count > 0)
            return WorkflowResult.Failure(WorkflowId, errors.ToArray());

        var context = new WorkflowContext
        {
            SessionId = request.SessionId,
            Parameters = request.Parameters,
            State = new NuzlockeState(),
            BattleContext = new BattleContext(),
            Result = new WorkflowResult { WorkflowId = WorkflowId, Success = true },
            Language = request.Language
        };

        try
        {
            await FetchDataAsync(context, ct);
            await MutateStateAsync(context, ct);
            context.Result.Advice = await GenerateAdviceAsync(context, ct);
            return context.Result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Workflow {WorkflowId} failed", WorkflowId);
            return WorkflowResult.Failure(WorkflowId, $"Workflow execution failed: {ex.Message}");
        }
    }

    protected override Task FetchDataAsync(WorkflowContext context, CancellationToken ct)
    {
        // No PokeAPI data needed for init
        return Task.CompletedTask;
    }

    protected override async Task MutateStateAsync(WorkflowContext context, CancellationToken ct)
    {
        var basePath = context.Parameters.GetString("base_path")!;
        var generation = context.Parameters.GetInt("generation") ?? 1;
        var lockeType = context.Parameters.GetString("locke_type") ?? "standard";

        var nuzlockeId = await _fileManager.CreateNuzlockeAsync(basePath, generation, lockeType);

        context.Result.Mutations.Add(new StateMutation
        {
            Type = "nuzlocke_created",
            Description = $"Created nuzlocke {nuzlockeId} (Gen {generation}, {lockeType})"
        });

        context.Result.Data["nuzlocke_id"] = nuzlockeId;
        context.Result.Data["generation"] = generation;
        context.Result.Data["locke_type"] = lockeType;
        context.Result.Data["base_path"] = basePath;

        // Store for advice generation
        context.FetchedData["nuzlocke_id"] = nuzlockeId;
    }

    protected override string GetSystemPrompt(WorkflowContext context)
    {
        var generation = context.Parameters.GetInt("generation") ?? 1;
        var lockeType = context.Parameters.GetString("locke_type") ?? "standard";

        return $"""
            You are a Pokemon Nuzlocke advisor specialized in Generation {generation} ({lockeType} rules).
            Give concise, actionable tips for starting a new Nuzlocke run.
            Focus on: starter choice, early game survival, and key first encounters.
            Keep the response under 250 words. And the answer must be {context.Language} language.
            """;
    }

    protected override string BuildUserMessage(WorkflowContext context)
    {
        var generation = context.Parameters.GetInt("generation") ?? 1;
        var lockeType = context.Parameters.GetString("locke_type") ?? "standard";

        return $"""
            I'm starting a new Generation {generation} Nuzlocke ({lockeType} rules).
            What starter should I choose and what should I watch out for in the early game?
            Responde me in {context.Language} language
            """;
    }
}
