using es.vargontoc.nuzlocke.ai.Agents;
using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;
using es.vargontoc.nuzlocke.ai.WebSockets;
using es.vargontoc.nuzlocke.ai.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests.WebSockets;

public class AdviceBackgroundDispatcherTests
{
    private readonly Mock<IAdviceConnectionManager> _mockConnectionManager = new();
    private readonly Mock<IAiProvider> _mockAiProvider = new();
    private readonly Mock<ILogger<AdviceBackgroundDispatcher>> _mockLogger = new();

    private AdviceBackgroundDispatcher CreateDispatcher()
    {
        var services = new ServiceCollection();
        services.AddScoped<IAiProvider>(_ => _mockAiProvider.Object);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        return new AdviceBackgroundDispatcher(_mockConnectionManager.Object, scopeFactory, _mockLogger.Object);
    }

    private AdviceBackgroundDispatcher CreateDispatcherWithAgent()
    {
        var mockState = new Mock<IStateManager>();
        mockState.Setup(s => s.GetStateAsync(It.IsAny<string>()))
            .ReturnsAsync(new NuzlockeState());
        mockState.Setup(s => s.GetStateAsync())
            .ReturnsAsync(new NuzlockeState());
        mockState.Setup(s => s.GetBattleContextAsync(It.IsAny<string>()))
            .ReturnsAsync(new BattleContext());
        mockState.Setup(s => s.GetBattleContextAsync())
            .ReturnsAsync(new BattleContext());

        var services = new ServiceCollection();
        services.AddScoped<IAiProvider>(_ => _mockAiProvider.Object);
        services.AddScoped<IStateManager>(_ => mockState.Object);
        services.AddScoped<IPokeApiConnector>(_ => new Mock<IPokeApiConnector>().Object);
        services.AddScoped<IWorkflowEngine>(_ => new Mock<IWorkflowEngine>().Object);
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddLogging();
        services.AddScoped<ToolExecutor>();
        services.AddScoped<NuzlockeAgent>();
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        return new AdviceBackgroundDispatcher(_mockConnectionManager.Object, scopeFactory, _mockLogger.Object);
    }

    [Fact]
    public async Task Dispatch_NoWebSocketConnection_SkipsAdvice()
    {
        _mockConnectionManager.Setup(c => c.HasConnection("session1")).Returns(false);
        var dispatcher = CreateDispatcher();

        dispatcher.Dispatch(new AdviceDispatchRequest
        {
            CorrelationId = "corr1",
            SessionId = "session1",
            WorkflowId = "test",
            SystemPrompt = "system",
            UserMessage = "user"
        });

        // Give time for fire-and-forget
        await Task.Delay(200);

        // Should not attempt to send anything
        _mockConnectionManager.Verify(
            c => c.SendAsync(It.IsAny<string>(), It.IsAny<AdviceWebSocketMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Dispatch_WithConnection_StreamsAdvice()
    {
        _mockConnectionManager.Setup(c => c.HasConnection("session1")).Returns(true);
        _mockConnectionManager.Setup(c => c.SendAsync("session1", It.IsAny<AdviceWebSocketMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockAiProvider.Setup(a => a.StreamCompletionAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncChunks("Hello ", "world!"));

        var dispatcher = CreateDispatcher();

        dispatcher.Dispatch(new AdviceDispatchRequest
        {
            CorrelationId = "corr1",
            SessionId = "session1",
            WorkflowId = "test",
            SystemPrompt = "system",
            UserMessage = "user"
        });

        await Task.Delay(500);

        // Verify: advice_start + 2 chunks + advice_end = 4 sends
        _mockConnectionManager.Verify(
            c => c.SendAsync("session1", It.Is<AdviceStartMessage>(m => m.Type == "advice_start"), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockConnectionManager.Verify(
            c => c.SendAsync("session1", It.Is<AdviceChunkMessage>(m => m.Type == "advice_chunk"), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        _mockConnectionManager.Verify(
            c => c.SendAsync("session1", It.Is<AdviceEndMessage>(m => m.Type == "advice_end" && m.FullAdvice == "Hello world!"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Dispatch_AiError_SendsAdviceError()
    {
        _mockConnectionManager.Setup(c => c.HasConnection("session1")).Returns(true);
        _mockConnectionManager.Setup(c => c.SendAsync("session1", It.IsAny<AdviceWebSocketMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockAiProvider.Setup(a => a.StreamCompletionAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncChunksThenError("LLM timeout"));

        var dispatcher = CreateDispatcher();

        dispatcher.Dispatch(new AdviceDispatchRequest
        {
            CorrelationId = "corr1",
            SessionId = "session1",
            WorkflowId = "test",
            SystemPrompt = "system",
            UserMessage = "user"
        });

        await Task.Delay(500);

        _mockConnectionManager.Verify(
            c => c.SendAsync("session1", It.Is<AdviceErrorMessage>(m => m.Type == "advice_error" && m.Error == "LLM timeout"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Dispatch_WebSocketDisconnectsMidStream_StopsStreaming()
    {
        _mockConnectionManager.Setup(c => c.HasConnection("session1")).Returns(true);

        // First send (advice_start) succeeds, second send (chunk) fails
        var sendCount = 0;
        _mockConnectionManager
            .Setup(c => c.SendAsync("session1", It.IsAny<AdviceWebSocketMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ++sendCount <= 1); // Only first send succeeds

        _mockAiProvider.Setup(a => a.StreamCompletionAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncChunks("chunk1", "chunk2", "chunk3"));

        var dispatcher = CreateDispatcher();

        dispatcher.Dispatch(new AdviceDispatchRequest
        {
            CorrelationId = "corr1",
            SessionId = "session1",
            WorkflowId = "test",
            SystemPrompt = "system",
            UserMessage = "user"
        });

        await Task.Delay(500);

        // advice_end should NOT have been sent
        _mockConnectionManager.Verify(
            c => c.SendAsync("session1", It.Is<AdviceEndMessage>(m => m.Type == "advice_end"), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DispatchAgentAdvice_NoWebSocketConnection_SkipsAdvice()
    {
        _mockConnectionManager.Setup(c => c.HasConnection("session1")).Returns(false);
        var dispatcher = CreateDispatcherWithAgent();

        dispatcher.DispatchAgentAdvice(new AgentAdviceDispatchRequest
        {
            CorrelationId = "corr_agent1",
            SessionId = "session1",
            Question = "What Pokemon should I catch?"
        });

        await Task.Delay(200);

        _mockConnectionManager.Verify(
            c => c.SendAsync(It.IsAny<string>(), It.IsAny<AdviceWebSocketMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DispatchAgentAdvice_WithConnection_SendsStartAndEnd()
    {
        _mockConnectionManager.Setup(c => c.HasConnection("session1")).Returns(true);
        _mockConnectionManager.Setup(c => c.SendAsync("session1", It.IsAny<AdviceWebSocketMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Mock AI to return a simple response without tool calls
        _mockAiProvider.Setup(a => a.GetCompletionWithToolsAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<ToolDefinition>>(),
                It.IsAny<List<ToolCallResult>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse { TextResponse = "Catch a Pikachu!" });

        var dispatcher = CreateDispatcherWithAgent();

        dispatcher.DispatchAgentAdvice(new AgentAdviceDispatchRequest
        {
            CorrelationId = "corr_agent2",
            SessionId = "session1",
            Question = "What Pokemon should I catch?"
        });

        await Task.Delay(500);

        _mockConnectionManager.Verify(
            c => c.SendAsync("session1",
                It.Is<AdviceStartMessage>(m => m.Type == "advice_start" && m.WorkflowId == "agent_advice"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockConnectionManager.Verify(
            c => c.SendAsync("session1",
                It.Is<AdviceEndMessage>(m => m.Type == "advice_end" && m.FullAdvice == "Catch a Pikachu!"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DispatchAgentAdvice_AgentError_SendsAdviceError()
    {
        _mockConnectionManager.Setup(c => c.HasConnection("session1")).Returns(true);
        _mockConnectionManager.Setup(c => c.SendAsync("session1", It.IsAny<AdviceWebSocketMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Mock AI to throw an exception
        _mockAiProvider.Setup(a => a.GetCompletionWithToolsAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<ToolDefinition>>(),
                It.IsAny<List<ToolCallResult>?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Ollama connection refused"));

        var dispatcher = CreateDispatcherWithAgent();

        dispatcher.DispatchAgentAdvice(new AgentAdviceDispatchRequest
        {
            CorrelationId = "corr_agent3",
            SessionId = "session1",
            Question = "Help me!"
        });

        await Task.Delay(500);

        _mockConnectionManager.Verify(
            c => c.SendAsync("session1",
                It.Is<AdviceErrorMessage>(m => m.Type == "advice_error" && m.Error.Contains("Ollama connection refused")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static async IAsyncEnumerable<string> AsyncChunks(params string[] chunks)
    {
        foreach (var chunk in chunks)
        {
            await Task.Yield();
            yield return chunk;
        }
    }

    private static async IAsyncEnumerable<string> AsyncChunksThenError(string errorMessage)
    {
        await Task.Yield();
        throw new InvalidOperationException(errorMessage);
        // Unreachable but required for async enumerable signature
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }
}
