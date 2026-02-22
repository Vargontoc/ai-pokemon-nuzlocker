using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;
using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Agents;
using es.vargontoc.nuzlocke.ai.Workflows;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests;

public class NuzlockeAgentFunctionCallingTests
{
    private readonly Mock<IAiProvider> _mockAiProvider;
    private readonly Mock<IStateManager> _mockStateManager;
    private readonly Mock<ToolExecutor> _mockToolExecutor;
    private readonly Mock<ILogger<NuzlockeAgent>> _mockLogger;
    private readonly NuzlockeAgent _agent;

    public NuzlockeAgentFunctionCallingTests()
    {
        _mockAiProvider = new Mock<IAiProvider>();
        _mockStateManager = new Mock<IStateManager>();
        _mockLogger = new Mock<ILogger<NuzlockeAgent>>();

        // Create mock for ToolExecutor
        var mockPokeApiConnector = new Mock<IPokeApiConnector>();
        var mockWorkflowEngine = new Mock<IWorkflowEngine>();
        var mockToolExecutorLogger = new Mock<ILogger<ToolExecutor>>();
        _mockToolExecutor = new Mock<ToolExecutor>(
            _mockStateManager.Object,
            mockPokeApiConnector.Object,
            mockWorkflowEngine.Object,
            mockToolExecutorLogger.Object);

        // Default setup for battle context (returns empty/no battle)
        _mockStateManager.Setup(m => m.GetBattleContextAsync()).ReturnsAsync(new BattleContext());
        _mockStateManager.Setup(m => m.GetBattleContextAsync(It.IsAny<string>())).ReturnsAsync(new BattleContext());

        _agent = new NuzlockeAgent(
            _mockAiProvider.Object,
            _mockStateManager.Object,
            _mockToolExecutor.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAdviceAsync_NoToolCalls_ReturnsDirectResponse()
    {
        // Arrange
        var gameState = new NuzlockeState
        {
            Team = new List<TeamMember>
            {
                new TeamMember { Nickname = "Sparky", Species = "pikachu", Level = 15 }
            }
        };

        _mockStateManager.Setup(m => m.GetStateAsync()).ReturnsAsync(gameState);

        var aiResponse = new AiResponse
        {
            TextResponse = "Your team looks good! Pikachu has good speed and special attack.",
            ToolCalls = new List<ToolCall>()
        };

        _mockAiProvider.Setup(p => p.GetCompletionWithToolsAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<IEnumerable<ToolDefinition>>(),
            It.IsAny<List<ToolCallResult>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(aiResponse);

        // Act
        var advice = await _agent.GetAdviceAsync("How is my team?");

        // Assert
        Assert.Contains("team looks good", advice);
        Assert.Contains("Pikachu", advice);
        _mockAiProvider.Verify(p => p.GetCompletionWithToolsAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<IEnumerable<ToolDefinition>>(),
            null,  // First call should have null tool results
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task GetAdviceAsync_WithToolCall_ExecutesToolAndContinues()
    {
        // Arrange
        var gameState = new NuzlockeState { Team = new List<TeamMember>() };
        _mockStateManager.Setup(m => m.GetStateAsync()).ReturnsAsync(gameState);

        // First response: AI wants to add a Pokemon
        var firstResponse = new AiResponse
        {
            TextResponse = "",
            ToolCalls = new List<ToolCall>
            {
                new ToolCall
                {
                    Id = "call_123",
                    Name = "add_to_team",
                    ArgumentsJson = "{\"nickname\":\"Sparky\",\"species\":\"pikachu\",\"level\":5,\"caughtAt\":\"Route 1\"}"
                }
            }
        };

        // Second response: AI responds after tool execution
        var secondResponse = new AiResponse
        {
            TextResponse = "I've added Sparky the Pikachu to your team!",
            ToolCalls = new List<ToolCall>()
        };

        var callCount = 0;
        _mockAiProvider.Setup(p => p.GetCompletionWithToolsAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<IEnumerable<ToolDefinition>>(),
            It.IsAny<List<ToolCallResult>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => callCount++ == 0 ? firstResponse : secondResponse);

        _mockToolExecutor.Setup(e => e.ExecuteAsync(It.IsAny<ToolCall>()))
            .ReturnsAsync(new ToolCallResult
            {
                ToolCallId = "call_123",
                ToolName = "add_to_team",
                Content = "{\"success\":true,\"message\":\"Added Sparky (pikachu) to the team\"}"
            });

        // Act
        var advice = await _agent.GetAdviceAsync("Add a Pikachu to my team");

        // Assert
        Assert.Contains("Sparky", advice);
        Assert.Contains("Pikachu", advice);

        // Verify AI was called twice (once for tool call, once for final response)
        _mockAiProvider.Verify(p => p.GetCompletionWithToolsAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<IEnumerable<ToolDefinition>>(),
            It.IsAny<List<ToolCallResult>>(),
            It.IsAny<CancellationToken>()
        ), Times.Exactly(2));

        // Verify tool was executed
        _mockToolExecutor.Verify(e => e.ExecuteAsync(
            It.Is<ToolCall>(tc => tc.Name == "add_to_team")
        ), Times.Once);
    }

    [Fact]
    public async Task GetAdviceAsync_MultipleToolCalls_ExecutesAll()
    {
        // Arrange
        var gameState = new NuzlockeState { Team = new List<TeamMember>() };
        _mockStateManager.Setup(m => m.GetStateAsync()).ReturnsAsync(gameState);

        // First response: AI wants to execute multiple tools
        var firstResponse = new AiResponse
        {
            ToolCalls = new List<ToolCall>
            {
                new ToolCall
                {
                    Id = "call_1",
                    Name = "record_encounter",
                    ArgumentsJson = "{\"location\":\"Route 1\",\"capturedSpecies\":\"pidgey\",\"capturedNickname\":\"Wings\"}"
                },
                new ToolCall
                {
                    Id = "call_2",
                    Name = "add_to_team",
                    ArgumentsJson = "{\"nickname\":\"Wings\",\"species\":\"pidgey\",\"level\":3,\"caughtAt\":\"Route 1\"}"
                }
            }
        };

        // Second response: Final advice
        var secondResponse = new AiResponse
        {
            TextResponse = "I've recorded the encounter and added Wings to your team!",
            ToolCalls = new List<ToolCall>()
        };

        var callCount = 0;
        _mockAiProvider.Setup(p => p.GetCompletionWithToolsAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<IEnumerable<ToolDefinition>>(),
            It.IsAny<List<ToolCallResult>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => callCount++ == 0 ? firstResponse : secondResponse);

        _mockToolExecutor.Setup(e => e.ExecuteAsync(It.IsAny<ToolCall>()))
            .ReturnsAsync((ToolCall tc) => new ToolCallResult
            {
                ToolCallId = tc.Id,
                ToolName = tc.Name,
                Content = "{\"success\":true}"
            });

        // Act
        var advice = await _agent.GetAdviceAsync("Catch the Pidgey I just encountered");

        // Assert
        Assert.Contains("Wings", advice);

        // Verify both tools were executed
        _mockToolExecutor.Verify(e => e.ExecuteAsync(
            It.Is<ToolCall>(tc => tc.Name == "record_encounter")
        ), Times.Once);

        _mockToolExecutor.Verify(e => e.ExecuteAsync(
            It.Is<ToolCall>(tc => tc.Name == "add_to_team")
        ), Times.Once);
    }

    [Fact]
    public async Task GetAdviceAsync_MaxCyclesReached_ReturnsMessage()
    {
        // Arrange
        var gameState = new NuzlockeState { Team = new List<TeamMember>() };
        _mockStateManager.Setup(m => m.GetStateAsync()).ReturnsAsync(gameState);

        // Always return a response with tool calls (will hit max cycles)
        var infiniteToolCallResponse = new AiResponse
        {
            TextResponse = "Processing...",
            ToolCalls = new List<ToolCall>
            {
                new ToolCall
                {
                    Id = "call_loop",
                    Name = "get_game_state",
                    ArgumentsJson = "{}"
                }
            }
        };

        _mockAiProvider.Setup(p => p.GetCompletionWithToolsAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<IEnumerable<ToolDefinition>>(),
            It.IsAny<List<ToolCallResult>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(infiniteToolCallResponse);

        _mockToolExecutor.Setup(e => e.ExecuteAsync(It.IsAny<ToolCall>()))
            .ReturnsAsync(new ToolCallResult
            {
                ToolCallId = "call_loop",
                ToolName = "get_game_state",
                Content = "{}"
            });

        // Act
        var advice = await _agent.GetAdviceAsync("Test infinite loop");

        // Assert
        // Should return the last text response before hitting the limit
        Assert.NotEmpty(advice);

        // Should have hit the cycle limit (10 calls)
        _mockAiProvider.Verify(p => p.GetCompletionWithToolsAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<IEnumerable<ToolDefinition>>(),
            It.IsAny<List<ToolCallResult>>(),
            It.IsAny<CancellationToken>()
        ), Times.Exactly(10));
    }
}

public class NuzlockeAgentStateContextTests
{
    private readonly Mock<IAiProvider> _mockAiProvider = new();
    private readonly Mock<IStateManager> _mockStateManager = new();
    private readonly Mock<ILogger<NuzlockeAgent>> _mockLogger = new();
    private readonly NuzlockeAgent _agent;

    public NuzlockeAgentStateContextTests()
    {
        var mockPokeApiConnector = new Mock<IPokeApiConnector>();
        var mockWorkflowEngine = new Mock<IWorkflowEngine>();
        var mockToolExecutorLogger = new Mock<ILogger<ToolExecutor>>();
        var mockToolExecutor = new Mock<ToolExecutor>(
            _mockStateManager.Object,
            mockPokeApiConnector.Object,
            mockWorkflowEngine.Object,
            mockToolExecutorLogger.Object);

        _mockStateManager.Setup(m => m.GetBattleContextAsync()).ReturnsAsync(new BattleContext());
        _mockStateManager.Setup(m => m.GetBattleContextAsync(It.IsAny<string>())).ReturnsAsync(new BattleContext());

        _mockAiProvider.Setup(p => p.GetCompletionWithToolsAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<IEnumerable<ToolDefinition>>(), It.IsAny<List<ToolCallResult>?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse { TextResponse = "advice", ToolCalls = new() });

        _agent = new NuzlockeAgent(
            _mockAiProvider.Object,
            _mockStateManager.Object,
            mockToolExecutor.Object,
            _mockLogger.Object);
    }

    private string CaptureUserMessage(Action setup)
    {
        var captured = string.Empty;
        _mockAiProvider.Setup(p => p.GetCompletionWithToolsAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<IEnumerable<ToolDefinition>>(), It.IsAny<List<ToolCallResult>?>(),
            It.IsAny<CancellationToken>()))
            .Callback<string, string, IEnumerable<ToolDefinition>, List<ToolCallResult>?, CancellationToken>(
                (_, msg, _, _, _) => captured = msg)
            .ReturnsAsync(new AiResponse { TextResponse = "advice", ToolCalls = new() });
        setup();
        return captured;
    }

    [Fact]
    public async Task GetAdviceAsync_WithLoadedState_PromptContainsCompactTeamAndItems()
    {
        var state = new NuzlockeState
        {
            Team = new List<TeamMember>
            {
                new() { Nickname = "Sparky", Species = "pikachu", Level = 15, CurrentHP = 35, MaxHP = 45 },
                new() { Nickname = "Blaze", Species = "charmander", Level = 12 }
            },
            PCStorage = new List<StoredPokemon>
            {
                new() { Nickname = "Caterpie", Species = "caterpie", Level = 5 }
            },
            DeadPokemon = new List<DeadPokemon>
            {
                new() { Nickname = "Pidgey", Species = "pidgey", Level = 6, DeathLocation = "Route 1" }
            },
            Inventory = new List<InventoryItem>
            {
                new() { Name = "Potion", Quantity = 3 },
                new() { Name = "Antidote", Quantity = 1 }
            },
            Encounters = new Dictionary<string, EncounterRecord>
            {
                ["Route 1"] = new() { Location = "Route 1", EncounterUsed = true, EncounterDate = DateTime.UtcNow }
            }
        };

        string capturedMsg = string.Empty;
        _mockStateManager.Setup(m => m.GetStateAsync()).ReturnsAsync(state);
        _mockAiProvider.Setup(p => p.GetCompletionWithToolsAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<IEnumerable<ToolDefinition>>(), It.IsAny<List<ToolCallResult>?>(),
            It.IsAny<CancellationToken>()))
            .Callback<string, string, IEnumerable<ToolDefinition>, List<ToolCallResult>?, CancellationToken>(
                (_, msg, _, _, _) => capturedMsg = msg)
            .ReturnsAsync(new AiResponse { TextResponse = "advice", ToolCalls = new() });

        await _agent.GetAdviceAsync("How is my team?");

        Assert.Contains("TEAM (2/6):", capturedMsg);
        Assert.Contains("Sparky/pikachu Lv15 [HP:35/45]", capturedMsg);
        Assert.Contains("Blaze/charmander Lv12", capturedMsg);
        Assert.Contains("PC (1):", capturedMsg);
        Assert.Contains("Caterpie/caterpie Lv5", capturedMsg);
        Assert.Contains("DEATHS (1):", capturedMsg);
        Assert.Contains("Pidgey/pidgey Lv6 @ Route 1", capturedMsg);
        Assert.Contains("ITEMS: Potion x3, Antidote x1", capturedMsg);
        Assert.Contains("LOCATION: Route 1", capturedMsg);
    }

    [Fact]
    public async Task GetAdviceAsync_WithEmptyState_PromptShowsNoneForAllSections()
    {
        _mockStateManager.Setup(m => m.GetStateAsync()).ReturnsAsync(new NuzlockeState());

        string capturedMsg = string.Empty;
        _mockAiProvider.Setup(p => p.GetCompletionWithToolsAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<IEnumerable<ToolDefinition>>(), It.IsAny<List<ToolCallResult>?>(),
            It.IsAny<CancellationToken>()))
            .Callback<string, string, IEnumerable<ToolDefinition>, List<ToolCallResult>?, CancellationToken>(
                (_, msg, _, _, _) => capturedMsg = msg)
            .ReturnsAsync(new AiResponse { TextResponse = "advice", ToolCalls = new() });

        await _agent.GetAdviceAsync("What should I do?");

        Assert.Contains("TEAM (0/6): none", capturedMsg);
        Assert.Contains("PC (0): none", capturedMsg);
        Assert.Contains("DEATHS (0): none", capturedMsg);
        Assert.Contains("ITEMS: none", capturedMsg);
        Assert.Contains("LOCATION: unknown", capturedMsg);
        Assert.Contains("BATTLE: none", capturedMsg);
    }

    [Fact]
    public async Task GetAdviceAsync_WithActiveBattle_PromptReflectsBattleInProgress()
    {
        _mockStateManager.Setup(m => m.GetStateAsync()).ReturnsAsync(new NuzlockeState());
        _mockStateManager.Setup(m => m.GetBattleContextAsync()).ReturnsAsync(new BattleContext
        {
            InBattle = true,
            OpponentName = "Brock/Onix",
            BattleType = "gym",
            ActivePokemonNickname = "Sparky",
            TurnCount = 3
        });

        string capturedMsg = string.Empty;
        _mockAiProvider.Setup(p => p.GetCompletionWithToolsAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<IEnumerable<ToolDefinition>>(), It.IsAny<List<ToolCallResult>?>(),
            It.IsAny<CancellationToken>()))
            .Callback<string, string, IEnumerable<ToolDefinition>, List<ToolCallResult>?, CancellationToken>(
                (_, msg, _, _, _) => capturedMsg = msg)
            .ReturnsAsync(new AiResponse { TextResponse = "advice", ToolCalls = new() });

        await _agent.GetAdviceAsync("What move should I use?");

        Assert.Contains("BATTLE: vs Brock/Onix", capturedMsg);
        Assert.Contains("gym", capturedMsg);
        Assert.Contains("leading:Sparky", capturedMsg);
        Assert.Contains("turn 3", capturedMsg);
    }
}
