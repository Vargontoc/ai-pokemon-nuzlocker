using es.vargontoc.nuzlocke.ai.Agents;
using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;
using es.vargontoc.nuzlocke.ai.Workflows;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests;

public class PersonalityPromptProviderTests
{
    private readonly IPersonalityPromptProvider _provider = new PersonalityPromptProvider();

    [Fact]
    public void GetPersonalityBlock_AllPersonalities_ReturnNonEmptyDistinctBlocks()
    {
        var personalities = Enum.GetValues<AgentPersonality>();
        var blocks = personalities.Select(p => _provider.GetPersonalityBlock(p)).ToList();

        // All blocks are non-empty
        Assert.All(blocks, b => Assert.False(string.IsNullOrWhiteSpace(b)));

        // All blocks are distinct
        var distinct = blocks.Distinct().ToList();
        Assert.Equal(personalities.Length, distinct.Count);
    }

    [Fact]
    public void GetPersonalityBlock_DefaultTechnical_ReturnsBlock()
    {
        var block = _provider.GetPersonalityBlock(AgentPersonality.Technical);

        Assert.False(string.IsNullOrWhiteSpace(block));
        Assert.Contains("Technical", block);
    }
}

public class NuzlockeAgentPersonalityIntegrationTests
{
    private readonly Mock<IAiProvider> _mockAiProvider = new();
    private readonly Mock<IStateManager> _mockStateManager = new();
    private readonly Mock<IPersonalityPromptProvider> _mockPersonality = new();

    private NuzlockeAgent CreateAgent()
    {
        var mockPokeApi = new Mock<IPokeApiConnector>();
        var mockWorkflowEngine = new Mock<IWorkflowEngine>();
        var mockToolExecutor = new Mock<ToolExecutor>(
            _mockStateManager.Object, mockPokeApi.Object,
            mockWorkflowEngine.Object, new Mock<ILogger<ToolExecutor>>().Object);

        _mockStateManager.Setup(m => m.GetStateAsync(It.IsAny<string>()))
            .ReturnsAsync(new NuzlockeState { Personality = AgentPersonality.Enthusiastic });
        _mockStateManager.Setup(m => m.GetBattleContextAsync(It.IsAny<string>()))
            .ReturnsAsync(new BattleContext());

        _mockPersonality.Setup(p => p.GetPersonalityBlock(AgentPersonality.Enthusiastic))
            .Returns("RESPONSE STYLE — Enthusiastic: YOU ARE ABSOLUTELY THRILLED!!");

        _mockAiProvider.Setup(p => p.GetCompletionWithToolsAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<IEnumerable<ToolDefinition>>(), It.IsAny<List<ToolCallResult>?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse { TextResponse = "AMAZING team!!", ToolCalls = new() });

        return new NuzlockeAgent(
            _mockAiProvider.Object,
            _mockStateManager.Object,
            mockToolExecutor.Object,
            new Mock<ILogger<NuzlockeAgent>>().Object,
            personalityProvider: _mockPersonality.Object);
    }

    [Fact]
    public async Task GetAdviceAsync_WithPersonality_SystemPromptContainsPersonalityBlock()
    {
        var agent = CreateAgent();
        string capturedSystemPrompt = string.Empty;

        _mockAiProvider.Setup(p => p.GetCompletionWithToolsAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<IEnumerable<ToolDefinition>>(), It.IsAny<List<ToolCallResult>?>(),
            It.IsAny<CancellationToken>()))
            .Callback<string, string, IEnumerable<ToolDefinition>, List<ToolCallResult>?, CancellationToken>(
                (sys, _, _, _, _) => capturedSystemPrompt = sys)
            .ReturnsAsync(new AiResponse { TextResponse = "AMAZING!!", ToolCalls = new() });

        await agent.GetAdviceAsync("How is my team?", sessionId: "session-1");

        Assert.Contains("RESPONSE STYLE — Enthusiastic", capturedSystemPrompt);
        Assert.Contains("YOU ARE ABSOLUTELY THRILLED", capturedSystemPrompt);
    }
}
