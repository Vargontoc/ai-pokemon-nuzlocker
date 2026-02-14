using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;
using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Agents;
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
        var mockToolExecutorLogger = new Mock<ILogger<ToolExecutor>>();
        _mockToolExecutor = new Mock<ToolExecutor>(
            _mockStateManager.Object,
            mockPokeApiConnector.Object,
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
