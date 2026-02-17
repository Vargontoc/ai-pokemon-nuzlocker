using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;
using es.vargontoc.nuzlocke.ai.Workflows;
using es.vargontoc.nuzlocke.ai.Workflows.Gameplay;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests.Workflows;

public class ItemObtainedWorkflowTests
{
    private readonly Mock<IStateManager> _mockState = new();
    private readonly Mock<IPokeApiConnector> _mockPokeApi = new();
    private readonly Mock<IAiProvider> _mockAi = new();
    private readonly Mock<INuzlockeFileManager> _mockFileManager = new();
    private readonly Mock<ILogger<ItemObtainedWorkflow>> _mockLogger = new();

    private NuzlockeState _currentState = new() { Generation = 1, LockeType = "standard" };

    public ItemObtainedWorkflowTests()
    {
        _mockFileManager.Setup(f => f.GetNuzlockePathAsync("test_nuzlocke_2026-02-15"))
            .ReturnsAsync("/tmp/nuzlockes/test_nuzlocke_2026-02-15");

        _mockState.Setup(s => s.GetStateAsync(It.IsAny<string>()))
            .ReturnsAsync(() => _currentState);
        _mockState.Setup(s => s.GetBattleContextAsync(It.IsAny<string>()))
            .ReturnsAsync(new BattleContext());

        _mockState.Setup(s => s.AddInventoryItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _mockPokeApi.Setup(p => p.GetItemAsync("potion"))
            .ReturnsAsync(new ItemData
            {
                Id = 17,
                Name = "potion",
                Category = new ItemCategory { Name = "medicine" },
                EffectEntries = new List<ItemEffectEntry>
                {
                    new()
                    {
                        ShortEffect = "Restores 20 HP.",
                        Effect = "Used on a party Pokémon. Restores 20 HP to a selected Pokémon.",
                        Language = new NamedApiResource { Name = "en" }
                    }
                }
            });

        _mockAi.Setup(a => a.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Save this potion for the Brock fight!");
    }

    private ItemObtainedWorkflow CreateWorkflow() =>
        new(_mockState.Object, _mockPokeApi.Object, _mockAi.Object, _mockFileManager.Object, _mockLogger.Object);

    private static WorkflowParameters MakeParams(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
        return new WorkflowParameters(dict);
    }

    private static WorkflowParameters DefaultParams() => MakeParams(new
    {
        nuzlocke_id = "test_nuzlocke_2026-02-15",
        item_name = "potion",
        quantity = 2,
        category = "potion",
        location = "Viridian City"
    });

    // --- Validation ---

    [Fact]
    public void Validate_AllParamsPresent_ReturnsNoErrors()
    {
        var workflow = CreateWorkflow();
        var errors = workflow.Validate(DefaultParams());
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_MissingNuzlockeId_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var errors = workflow.Validate(MakeParams(new { item_name = "potion", category = "potion" }));
        Assert.Contains(errors, e => e.Contains("nuzlocke_id"));
    }

    [Fact]
    public void Validate_MissingItemName_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var errors = workflow.Validate(MakeParams(new { nuzlocke_id = "test", category = "potion" }));
        Assert.Contains(errors, e => e.Contains("item_name"));
    }

    [Fact]
    public void Validate_MissingCategory_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var errors = workflow.Validate(MakeParams(new { nuzlocke_id = "test", item_name = "potion" }));
        Assert.Contains(errors, e => e.Contains("category"));
    }

    // --- BuildContextAsync: nuzlocke not found ---

    [Fact]
    public async Task Execute_NuzlockeNotFound_ReturnsFailure()
    {
        _mockFileManager.Setup(f => f.GetNuzlockePathAsync(It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        var workflow = CreateWorkflow();
        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "item_obtained",
            SessionId = "s1",
            Parameters = DefaultParams()
        });

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Nuzlocke not found"));
    }

    // --- FetchData ---

    [Fact]
    public async Task Execute_FetchData_CallsPokeApiForItem()
    {
        var workflow = CreateWorkflow();
        await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "item_obtained",
            SessionId = "s1",
            Parameters = DefaultParams()
        });

        _mockPokeApi.Verify(p => p.GetItemAsync("potion"), Times.Once);
    }

    [Fact]
    public async Task Execute_FetchData_ItemNotInPokeApi_StillSucceeds()
    {
        _mockPokeApi.Setup(p => p.GetItemAsync("custom-berry"))
            .ThrowsAsync(new HttpRequestException("Not found"));

        var workflow = CreateWorkflow();
        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "item_obtained",
            SessionId = "s1",
            Parameters = MakeParams(new
            {
                nuzlocke_id = "test_nuzlocke_2026-02-15",
                item_name = "custom-berry",
                category = "other"
            })
        });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Execute_FetchData_ItemReturnsNull_StillSucceeds()
    {
        _mockPokeApi.Setup(p => p.GetItemAsync("mystery-item"))
            .ReturnsAsync((ItemData?)null);

        var workflow = CreateWorkflow();
        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "item_obtained",
            SessionId = "s1",
            Parameters = MakeParams(new
            {
                nuzlocke_id = "test_nuzlocke_2026-02-15",
                item_name = "mystery-item",
                category = "key"
            })
        });

        Assert.True(result.Success);
    }

    // --- MutateState ---

    [Fact]
    public async Task Execute_MutateState_CallsAddInventoryItemAsync()
    {
        var workflow = CreateWorkflow();
        await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "item_obtained",
            SessionId = "s1",
            Parameters = DefaultParams()
        });

        _mockState.Verify(s => s.AddInventoryItemAsync("test_nuzlocke_2026-02-15", "potion", 2, "potion"), Times.Once);
    }

    [Fact]
    public async Task Execute_MutateState_DefaultQuantityIs1()
    {
        var workflow = CreateWorkflow();
        await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "item_obtained",
            SessionId = "s1",
            Parameters = MakeParams(new
            {
                nuzlocke_id = "test_nuzlocke_2026-02-15",
                item_name = "potion",
                category = "potion"
                // no quantity — should default to 1
            })
        });

        _mockState.Verify(s => s.AddInventoryItemAsync("test_nuzlocke_2026-02-15", "potion", 1, "potion"), Times.Once);
    }

    [Fact]
    public async Task Execute_MutateState_MutationRecorded()
    {
        var workflow = CreateWorkflow();
        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "item_obtained",
            SessionId = "s1",
            Parameters = DefaultParams()
        });

        Assert.True(result.Success);
        Assert.Contains(result.Mutations, m => m.Type == "item_added");
        Assert.Contains(result.Mutations, m => m.Description.Contains("potion"));
    }

    // --- GenerateAdvice ---

    [Fact]
    public async Task Execute_GeneratesAdvice()
    {
        var workflow = CreateWorkflow();
        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "item_obtained",
            SessionId = "s1",
            Parameters = DefaultParams()
        });

        Assert.Equal("Save this potion for the Brock fight!", result.Advice);
    }

    [Fact]
    public async Task Execute_AdvicePromptContainsTeamAndInventoryAndItem()
    {
        string? capturedSystem = null;
        string? capturedUser = null;

        _currentState = new NuzlockeState
        {
            Generation = 1,
            LockeType = "standard",
            Team = new List<TeamMember>
            {
                new() { Nickname = "Blaze", Species = "charmander", Level = 12, Moves = new List<string> { "scratch", "ember" } }
            },
            Inventory = new List<InventoryItem>
            {
                new() { Name = "pokeball", Quantity = 5, Category = "pokeball" }
            }
        };

        _mockAi.Setup(a => a.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((sys, usr, _) => { capturedSystem = sys; capturedUser = usr; })
            .ReturnsAsync("Use it wisely!");

        var workflow = CreateWorkflow();
        await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "item_obtained",
            SessionId = "s1",
            Parameters = DefaultParams()
        });

        Assert.NotNull(capturedSystem);
        Assert.Contains("Generation 1", capturedSystem!);
        Assert.Contains("item", capturedSystem!, StringComparison.OrdinalIgnoreCase);

        Assert.NotNull(capturedUser);
        Assert.Contains("potion", capturedUser!);        // item obtained
        Assert.Contains("Blaze", capturedUser!);          // team
        Assert.Contains("pokeball", capturedUser!);       // existing inventory
    }

    // --- Result.Data ---

    [Fact]
    public async Task Execute_ResultDataContainsItemInfoAndInventory()
    {
        _currentState = new NuzlockeState
        {
            Generation = 1,
            LockeType = "standard",
            Inventory = new List<InventoryItem>
            {
                new() { Name = "potion", Quantity = 3, Category = "potion" }
            }
        };

        var workflow = CreateWorkflow();
        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "item_obtained",
            SessionId = "s1",
            Parameters = DefaultParams()
        });

        Assert.True(result.Success);
        Assert.True(result.Data.ContainsKey("item"));
        Assert.True(result.Data.ContainsKey("inventory"));
    }

    // --- Validation fails → no API calls ---

    [Fact]
    public async Task Execute_ValidationFails_NoApiCalls()
    {
        var workflow = CreateWorkflow();
        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "item_obtained",
            SessionId = "s1",
            Parameters = MakeParams(new { }) // empty
        });

        Assert.False(result.Success);
        _mockPokeApi.Verify(p => p.GetItemAsync(It.IsAny<string>()), Times.Never);
        _mockState.Verify(s => s.AddInventoryItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    // --- ExecuteDeterministicAsync ---

    [Fact]
    public async Task ExecuteDeterministic_ReturnsPromptsWithoutCallingLLM()
    {
        var workflow = CreateWorkflow();
        var result = await workflow.ExecuteDeterministicAsync(new WorkflowRequest
        {
            WorkflowId = "item_obtained",
            SessionId = "s1",
            Parameters = DefaultParams()
        });

        Assert.True(result.Result.Success);
        Assert.NotNull(result.SystemPrompt);
        Assert.NotNull(result.UserMessage);
        Assert.Contains("potion", result.UserMessage!);

        _mockAi.Verify(a => a.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
