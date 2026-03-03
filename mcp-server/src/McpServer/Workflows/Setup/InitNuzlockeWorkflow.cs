using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;

namespace es.vargontoc.nuzlocke.ai.Workflows.Setup;

/// <summary>
/// Workflow de inicialización de una partida Nuzlocke. Mutación pura, sin llamada al LLM.
/// Se ejecuta en la primera conexión WebSocket al nuzlocke.
/// Idempotente: si ya está inicializado, añade una mutation de warning y no hace nada más.
/// </summary>
public class InitNuzlockeWorkflow : WorkflowBase
{
    private readonly INuzlockeRepository _repository;
    private const string AlreadyInitializedKey = "_already_initialized";

    public override string WorkflowId => "init_nuzlocke";

    public InitNuzlockeWorkflow(
        IStateManager stateManager,
        IPokeApiConnector pokeApi,
        IAiProvider aiProvider,
        INuzlockeRepository repository,
        ILogger<InitNuzlockeWorkflow> logger)
        : base(stateManager, pokeApi, aiProvider, logger)
    {
        _repository = repository;
    }

    public override IReadOnlyList<string> Validate(WorkflowParameters parameters) => [];

    protected override async Task<(WorkflowContext? Context, WorkflowResult? FailureResult)> BuildContextAsync(
        WorkflowRequest request, CancellationToken ct)
    {
        var metadata = await _repository.GetMetadataAsync(request.NuzlockeId);
        if (metadata == null)
        {
            return (null, WorkflowResult.Failure(WorkflowId,
                $"Nuzlocke not found: '{request.NuzlockeId}'. Create it first via POST /nuzlocke"));
        }

        var state = await _repository.GetGameStateAsync(request.NuzlockeId);
        var battleContext = await _repository.GetBattleStateAsync(request.NuzlockeId) ?? new BattleContext();

        var context = new WorkflowContext
        {
            NuzlockeId = request.NuzlockeId,
            Parameters = request.Parameters,
            State = state,
            BattleContext = battleContext,
            Result = new WorkflowResult { WorkflowId = WorkflowId, Success = true },
            Language = request.Language
        };

        context.FetchedData["_metadata"] = metadata;
        return (context, null);
    }

    protected override Task FetchDataAsync(WorkflowContext context, CancellationToken ct) =>
        Task.CompletedTask;

    protected override async Task MutateStateAsync(WorkflowContext context, CancellationToken ct)
    {
        var metadata = (NuzlockeMetadata)context.FetchedData["_metadata"];

        if (metadata.IsInitialized)
        {
            Logger.LogWarning(
                "Nuzlocke {NuzlockeId} is already initialized (Gen {Generation}, {LockeType}). Skipping init.",
                context.NuzlockeId, metadata.Generation, metadata.LockeType);

            context.Result.Mutations.Add(new StateMutation
            {
                Type = "nuzlocke_already_initialized",
                Description = $"Nuzlocke {context.NuzlockeId} already initialized (Gen {metadata.Generation}, {metadata.LockeType})"
            });

            context.FetchedData[AlreadyInitializedKey] = true;
            return;
        }

        // Primera inicialización: crear game_state.json y marcar como inicializado
        var initialState = new NuzlockeState
        {
            Generation = metadata.Generation,
            LockeType = metadata.LockeType.ToString().ToLowerInvariant(),
            StartDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        await _repository.SaveGameStateAsync(context.NuzlockeId, initialState);

        metadata.IsInitialized = true;
        await _repository.SaveMetadataAsync(metadata);

        await _repository.UpdateStatusAsync(context.NuzlockeId, NuzlockeStatus.Active);

        context.State = initialState;

        context.Result.Mutations.Add(new StateMutation
        {
            Type = "nuzlocke_initialized",
            Description = $"Initialized nuzlocke {context.NuzlockeId} (Gen {metadata.Generation}, {metadata.LockeType})"
        });

        context.Result.Data["nuzlocke_id"] = context.NuzlockeId;
        context.Result.Data["generation"] = metadata.Generation;
        context.Result.Data["locke_type"] = metadata.LockeType.ToString();
    }

    // Sin LLM: GenerateAdviceAsync devuelve null (hereda el comportamiento base que skip si no hay prompts)
    protected override Task<string?> GenerateAdviceAsync(WorkflowContext context, CancellationToken ct) =>
        Task.FromResult<string?>(null);

    protected override string GetSystemPrompt(WorkflowContext context) => string.Empty;

    protected override string BuildUserMessage(WorkflowContext context) => string.Empty;
}
