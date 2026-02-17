using System.Text;
using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;

namespace es.vargontoc.nuzlocke.ai.Workflows.Gameplay;

/// <summary>
/// Workflow para registrar la obtención de un item.
/// Busca datos del item en PokeAPI (si existe), lo añade al inventario,
/// y genera consejo sobre cuándo y cómo usarlo.
/// </summary>
public class ItemObtainedWorkflow : WorkflowBase
{
    private readonly INuzlockeFileManager _fileManager;

    public override string WorkflowId => "item_obtained";

    public ItemObtainedWorkflow(
        IStateManager stateManager,
        IPokeApiConnector pokeApi,
        IAiProvider aiProvider,
        INuzlockeFileManager fileManager,
        ILogger<ItemObtainedWorkflow> logger)
        : base(stateManager, pokeApi, aiProvider, logger)
    {
        _fileManager = fileManager;
    }

    public override IReadOnlyList<string> Validate(WorkflowParameters parameters)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(parameters.GetString("nuzlocke_id")))
            errors.Add("Missing required parameter: nuzlocke_id");

        if (string.IsNullOrWhiteSpace(parameters.GetString("item_name")))
            errors.Add("Missing required parameter: item_name");

        if (string.IsNullOrWhiteSpace(parameters.GetString("category")))
            errors.Add("Missing required parameter: category");

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
                $"Nuzlocke not found: {nuzlockeId}. Ensure init_nuzlocke was called and the server has discovered this nuzlocke."));
        }

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
        var itemName = context.Parameters.GetString("item_name")!;

        // Try fetching from PokeAPI — item may not exist (custom items are valid)
        try
        {
            var itemData = await PokeApi.GetItemAsync(itemName);
            if (itemData != null)
            {
                context.FetchedData["item"] = itemData;
                context.Result.Data["item_api_data"] = new
                {
                    id = itemData.Id,
                    name = itemData.Name,
                    category = itemData.Category.Name,
                    effect = itemData.EffectEntries
                        .FirstOrDefault(e => e.Language.Name == "en")?.ShortEffect
                };
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to fetch PokeAPI data for item {ItemName}, continuing without external data", itemName);
        }
    }

    protected override async Task MutateStateAsync(WorkflowContext context, CancellationToken ct)
    {
        var itemName = context.Parameters.GetString("item_name")!;
        var quantity = context.Parameters.GetInt("quantity") ?? 1;
        var category = context.Parameters.GetString("category")!;

        await StateManager.AddInventoryItemAsync(context.SessionId, itemName, quantity, category);

        context.Result.Mutations.Add(new StateMutation
        {
            Type = "item_added",
            Description = $"Added {quantity}x {itemName} ({category}) to inventory"
        });

        // Re-read state to get updated inventory
        context.State = await StateManager.GetStateAsync(context.SessionId);

        // Add item info and updated inventory to result data
        context.Result.Data["item"] = new
        {
            name = itemName,
            quantity,
            category,
            location = context.Parameters.GetString("location")
        };
        context.Result.Data["inventory"] = context.State.Inventory;
    }

    protected override string GetSystemPrompt(WorkflowContext context)
    {
        return $"""
            You are a Pokemon Nuzlocke advisor for Generation {context.State.Generation} ({context.State.LockeType} rules).
            The player just obtained an item. Advise on when and how to use it considering:
            - Current team composition, levels, and HP
            - Existing inventory (don't waste if they already have many)
            - Key items to save for upcoming challenges (potions for gyms, etc.)
            - Nuzlocke-specific strategy (items are precious when Pokemon can die permanently)
            Keep the response under 200 words.
            You answer in {context.Language} language.
            """;
    }

    protected override string BuildUserMessage(WorkflowContext context)
    {
        var itemName = context.Parameters.GetString("item_name")!;
        var quantity = context.Parameters.GetInt("quantity") ?? 1;
        var category = context.Parameters.GetString("category")!;
        var location = context.Parameters.GetString("location");

        var sb = new StringBuilder();
        sb.AppendLine($"I obtained {quantity}x {itemName} (category: {category}).");

        if (!string.IsNullOrEmpty(location))
            sb.AppendLine($"Found at: {location}");

        // Add PokeAPI data if available
        if (context.FetchedData.TryGetValue("item", out var itemObj) && itemObj is ItemData itemData)
        {
            var effect = itemData.EffectEntries
                .FirstOrDefault(e => e.Language.Name == "en")?.ShortEffect;
            if (!string.IsNullOrEmpty(effect))
                sb.AppendLine($"Effect: {effect}");
        }

        sb.AppendLine();
        sb.AppendLine(BuildTeamSummary(context.State));
        sb.AppendLine(BuildInventorySummary(context.State));
        sb.AppendLine();
        sb.AppendLine("When should I use this item? Should I save it for later?");
        sb.Append($"Respond in {context.Language} language");

        return sb.ToString();
    }

    private static string BuildInventorySummary(NuzlockeState state)
    {
        if (state.Inventory.Count == 0) return "Inventory: empty";
        var sb = new StringBuilder();
        sb.AppendLine($"Inventory ({state.Inventory.Count} items):");
        foreach (var item in state.Inventory)
        {
            sb.AppendLine($"  - {item.Name} x{item.Quantity} ({item.Category})");
        }
        return sb.ToString();
    }
}
