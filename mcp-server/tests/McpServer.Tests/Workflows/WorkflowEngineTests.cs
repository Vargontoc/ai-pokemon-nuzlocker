using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;
using es.vargontoc.nuzlocke.ai.WebSockets;
using es.vargontoc.nuzlocke.ai.Workflows;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests.Workflows;

public class WorkflowEngineTests
{
    private readonly Mock<ILogger<WorkflowEngine>> _mockLogger = new();
    private readonly Mock<IAdviceConnectionManager> _mockConnectionManager = new();
    private readonly Mock<IAdviceDispatcher> _mockDispatcher = new();

    [Fact]
    public async Task Execute_UnknownWorkflow_ReturnsFailure()
    {
        var engine = new WorkflowEngine(Array.Empty<IWorkflow>(), _mockConnectionManager.Object, _mockDispatcher.Object, _mockLogger.Object);

        var result = await engine.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "nonexistent",
            NuzlockeId = "s1"
        });

        Assert.False(result.Success);
        Assert.Contains("Unknown workflow", result.Errors[0]);
    }

    [Fact]
    public async Task Execute_DispatchesToCorrectWorkflow()
    {
        var mockWorkflow = new Mock<IWorkflow>();
        mockWorkflow.Setup(w => w.WorkflowId).Returns("test_workflow");
        mockWorkflow.Setup(w => w.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowResult { WorkflowId = "test_workflow", Success = true });

        var engine = new WorkflowEngine(new[] { mockWorkflow.Object }, _mockConnectionManager.Object, _mockDispatcher.Object, _mockLogger.Object);

        var result = await engine.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "test_workflow",
            NuzlockeId = "s1"
        });

        Assert.True(result.Success);
        Assert.Equal("test_workflow", result.WorkflowId);
        mockWorkflow.Verify(w => w.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_IsCaseInsensitive()
    {
        var mockWorkflow = new Mock<IWorkflow>();
        mockWorkflow.Setup(w => w.WorkflowId).Returns("capture_pokemon");
        mockWorkflow.Setup(w => w.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowResult { WorkflowId = "capture_pokemon", Success = true });

        var engine = new WorkflowEngine(new[] { mockWorkflow.Object }, _mockConnectionManager.Object, _mockDispatcher.Object, _mockLogger.Object);

        var result = await engine.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "CAPTURE_POKEMON",
            NuzlockeId = "s1"
        });

        Assert.True(result.Success);
    }

    [Fact]
    public void GetAvailableWorkflows_ReturnsAllRegistered()
    {
        var w1 = new Mock<IWorkflow>();
        w1.Setup(w => w.WorkflowId).Returns("workflow_a");
        var w2 = new Mock<IWorkflow>();
        w2.Setup(w => w.WorkflowId).Returns("workflow_b");

        var engine = new WorkflowEngine(new[] { w1.Object, w2.Object }, _mockConnectionManager.Object, _mockDispatcher.Object, _mockLogger.Object);

        var available = engine.GetAvailableWorkflows();
        Assert.Equal(2, available.Count);
        Assert.Contains("workflow_a", available);
        Assert.Contains("workflow_b", available);
    }

    [Fact]
    public void GetAvailableWorkflows_EmptyWhenNoWorkflows()
    {
        var engine = new WorkflowEngine(Array.Empty<IWorkflow>(), _mockConnectionManager.Object, _mockDispatcher.Object, _mockLogger.Object);
        Assert.Empty(engine.GetAvailableWorkflows());
    }

    [Fact]
    public async Task Execute_MultipleWorkflows_DispatchesCorrectly()
    {
        var w1 = new Mock<IWorkflow>();
        w1.Setup(w => w.WorkflowId).Returns("alpha");
        w1.Setup(w => w.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowResult { WorkflowId = "alpha", Success = true, Advice = "Alpha advice" });

        var w2 = new Mock<IWorkflow>();
        w2.Setup(w => w.WorkflowId).Returns("beta");
        w2.Setup(w => w.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowResult { WorkflowId = "beta", Success = true, Advice = "Beta advice" });

        var engine = new WorkflowEngine(new[] { w1.Object, w2.Object }, _mockConnectionManager.Object, _mockDispatcher.Object, _mockLogger.Object);

        var r1 = await engine.ExecuteAsync(new WorkflowRequest { WorkflowId = "alpha", NuzlockeId = "s1" });
        var r2 = await engine.ExecuteAsync(new WorkflowRequest { WorkflowId = "beta", NuzlockeId = "s1" });

        Assert.Equal("Alpha advice", r1.Advice);
        Assert.Equal("Beta advice", r2.Advice);
        w1.Verify(w => w.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        w2.Verify(w => w.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteWithAsyncAdvice_NoWebSocket_FallsBackToSync()
    {
        var mockWorkflow = new Mock<IWorkflow>();
        mockWorkflow.Setup(w => w.WorkflowId).Returns("test_workflow");
        mockWorkflow.Setup(w => w.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowResult { WorkflowId = "test_workflow", Success = true, Advice = "sync advice" });

        _mockConnectionManager.Setup(c => c.HasConnection(It.IsAny<string>())).Returns(false);

        var engine = new WorkflowEngine(new[] { mockWorkflow.Object }, _mockConnectionManager.Object, _mockDispatcher.Object, _mockLogger.Object);

        var result = await engine.ExecuteWithAsyncAdviceAsync(new WorkflowRequest
        {
            WorkflowId = "test_workflow",
            NuzlockeId = "s1"
        });

        Assert.True(result.Success);
        Assert.Equal("sync advice", result.Advice);
        Assert.Null(result.CorrelationId);
        mockWorkflow.Verify(w => w.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        mockWorkflow.Verify(w => w.ExecuteDeterministicAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteWithAsyncAdvice_WithWebSocket_DispatchesAdvice()
    {
        var mockWorkflow = new Mock<IWorkflow>();
        mockWorkflow.Setup(w => w.WorkflowId).Returns("test_workflow");
        mockWorkflow.Setup(w => w.ExecuteDeterministicAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeterministicResult
            {
                Result = new WorkflowResult { WorkflowId = "test_workflow", Success = true },
                SystemPrompt = "system prompt",
                UserMessage = "user message"
            });

        _mockConnectionManager.Setup(c => c.HasConnection("s1")).Returns(true);
        _mockConnectionManager.Setup(c => c.SendAsync("s1", It.IsAny<AdviceWebSocketMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var engine = new WorkflowEngine(new[] { mockWorkflow.Object }, _mockConnectionManager.Object, _mockDispatcher.Object, _mockLogger.Object);

        var result = await engine.ExecuteWithAsyncAdviceAsync(new WorkflowRequest
        {
            WorkflowId = "test_workflow",
            NuzlockeId = "s1"
        });

        Assert.True(result.Success);
        Assert.Null(result.Advice); // Advice is async, not in HTTP response
        Assert.NotNull(result.CorrelationId);
        _mockDispatcher.Verify(d => d.Dispatch(It.Is<AdviceDispatchRequest>(r =>
            r.NuzlockeId == "s1" &&
            r.SystemPrompt == "system prompt" &&
            r.UserMessage == "user message")), Times.Once);
    }

    [Fact]
    public async Task ExecuteWithAsyncAdvice_UnknownWorkflow_ReturnsFailure()
    {
        var engine = new WorkflowEngine(Array.Empty<IWorkflow>(), _mockConnectionManager.Object, _mockDispatcher.Object, _mockLogger.Object);

        var result = await engine.ExecuteWithAsyncAdviceAsync(new WorkflowRequest
        {
            WorkflowId = "nonexistent",
            NuzlockeId = "s1"
        });

        Assert.False(result.Success);
        Assert.Contains("Unknown workflow", result.Errors[0]);
    }

    [Fact]
    public async Task ExecuteWithAsyncAdvice_DeterministicFails_ReturnsFailureNoDispatch()
    {
        var mockWorkflow = new Mock<IWorkflow>();
        mockWorkflow.Setup(w => w.WorkflowId).Returns("test_workflow");
        mockWorkflow.Setup(w => w.ExecuteDeterministicAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeterministicResult
            {
                Result = WorkflowResult.Failure("test_workflow", "Validation error")
            });

        _mockConnectionManager.Setup(c => c.HasConnection("s1")).Returns(true);
        _mockConnectionManager.Setup(c => c.SendAsync("s1", It.IsAny<AdviceWebSocketMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var engine = new WorkflowEngine(new[] { mockWorkflow.Object }, _mockConnectionManager.Object, _mockDispatcher.Object, _mockLogger.Object);

        var result = await engine.ExecuteWithAsyncAdviceAsync(new WorkflowRequest
        {
            WorkflowId = "test_workflow",
            NuzlockeId = "s1"
        });

        Assert.False(result.Success);
        _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<AdviceDispatchRequest>()), Times.Never);

        // Should still emit workflow_event for the failure
        _mockConnectionManager.Verify(c => c.SendAsync("s1",
            It.Is<WorkflowEventMessage>(m => m.Type == "workflow_event" && !m.Success),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Event emission tests ---

    [Fact]
    public async Task ExecuteAsync_WithWebSocket_EmitsWorkflowEvent()
    {
        var mockWorkflow = new Mock<IWorkflow>();
        mockWorkflow.Setup(w => w.WorkflowId).Returns("test_workflow");
        mockWorkflow.Setup(w => w.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowResult
            {
                WorkflowId = "test_workflow",
                Success = true,
                Mutations = new List<StateMutation>
                {
                    new() { Type = "added_to_team", Description = "Sparky added to team" }
                }
            });

        _mockConnectionManager.Setup(c => c.HasConnection("s1")).Returns(true);
        _mockConnectionManager.Setup(c => c.SendAsync("s1", It.IsAny<AdviceWebSocketMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var engine = new WorkflowEngine(new[] { mockWorkflow.Object }, _mockConnectionManager.Object, _mockDispatcher.Object, _mockLogger.Object);

        var result = await engine.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "test_workflow",
            NuzlockeId = "s1"
        });

        Assert.True(result.Success);
        _mockConnectionManager.Verify(c => c.SendAsync("s1",
            It.Is<WorkflowEventMessage>(m =>
                m.Type == "workflow_event" &&
                m.WorkflowId == "test_workflow" &&
                m.Success &&
                m.Mutations.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_NoWebSocket_DoesNotEmitEvent()
    {
        var mockWorkflow = new Mock<IWorkflow>();
        mockWorkflow.Setup(w => w.WorkflowId).Returns("test_workflow");
        mockWorkflow.Setup(w => w.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowResult { WorkflowId = "test_workflow", Success = true });

        _mockConnectionManager.Setup(c => c.HasConnection(It.IsAny<string>())).Returns(false);

        var engine = new WorkflowEngine(new[] { mockWorkflow.Object }, _mockConnectionManager.Object, _mockDispatcher.Object, _mockLogger.Object);

        await engine.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "test_workflow",
            NuzlockeId = "s1"
        });

        _mockConnectionManager.Verify(
            c => c.SendAsync(It.IsAny<string>(), It.IsAny<AdviceWebSocketMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WorkflowFails_EmitsErrorEvent()
    {
        var mockWorkflow = new Mock<IWorkflow>();
        mockWorkflow.Setup(w => w.WorkflowId).Returns("test_workflow");
        mockWorkflow.Setup(w => w.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowResult.Failure("test_workflow", "Something went wrong"));

        _mockConnectionManager.Setup(c => c.HasConnection("s1")).Returns(true);
        _mockConnectionManager.Setup(c => c.SendAsync("s1", It.IsAny<AdviceWebSocketMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var engine = new WorkflowEngine(new[] { mockWorkflow.Object }, _mockConnectionManager.Object, _mockDispatcher.Object, _mockLogger.Object);

        var result = await engine.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "test_workflow",
            NuzlockeId = "s1"
        });

        Assert.False(result.Success);
        _mockConnectionManager.Verify(c => c.SendAsync("s1",
            It.Is<WorkflowEventMessage>(m =>
                m.Type == "workflow_event" &&
                !m.Success &&
                m.Errors.Contains("Something went wrong")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteWithAsyncAdvice_Success_EmitsEventAndDispatchesAdvice()
    {
        var mockWorkflow = new Mock<IWorkflow>();
        mockWorkflow.Setup(w => w.WorkflowId).Returns("test_workflow");
        mockWorkflow.Setup(w => w.ExecuteDeterministicAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeterministicResult
            {
                Result = new WorkflowResult
                {
                    WorkflowId = "test_workflow",
                    Success = true,
                    Mutations = new List<StateMutation>
                    {
                        new() { Type = "captured", Description = "Caught Pikachu" }
                    }
                },
                SystemPrompt = "system",
                UserMessage = "user"
            });

        _mockConnectionManager.Setup(c => c.HasConnection("s1")).Returns(true);
        _mockConnectionManager.Setup(c => c.SendAsync("s1", It.IsAny<AdviceWebSocketMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var engine = new WorkflowEngine(new[] { mockWorkflow.Object }, _mockConnectionManager.Object, _mockDispatcher.Object, _mockLogger.Object);

        await engine.ExecuteWithAsyncAdviceAsync(new WorkflowRequest
        {
            WorkflowId = "test_workflow",
            NuzlockeId = "s1"
        });

        // Both event and advice dispatch should happen
        _mockConnectionManager.Verify(c => c.SendAsync("s1",
            It.Is<WorkflowEventMessage>(m => m.Type == "workflow_event" && m.Success),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockDispatcher.Verify(d => d.Dispatch(It.IsAny<AdviceDispatchRequest>()), Times.Once);
    }

    [Fact]
    public async Task EmitWorkflowEvent_SendFails_DoesNotThrow()
    {
        var mockWorkflow = new Mock<IWorkflow>();
        mockWorkflow.Setup(w => w.WorkflowId).Returns("test_workflow");
        mockWorkflow.Setup(w => w.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowResult { WorkflowId = "test_workflow", Success = true });

        _mockConnectionManager.Setup(c => c.HasConnection("s1")).Returns(true);
        _mockConnectionManager.Setup(c => c.SendAsync("s1", It.IsAny<AdviceWebSocketMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Send fails

        var engine = new WorkflowEngine(new[] { mockWorkflow.Object }, _mockConnectionManager.Object, _mockDispatcher.Object, _mockLogger.Object);

        // Should not throw even though SendAsync returns false
        var result = await engine.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "test_workflow",
            NuzlockeId = "s1"
        });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_NoCorrelationId_GeneratesOneForEvent()
    {
        var mockWorkflow = new Mock<IWorkflow>();
        mockWorkflow.Setup(w => w.WorkflowId).Returns("test_workflow");
        mockWorkflow.Setup(w => w.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowResult { WorkflowId = "test_workflow", Success = true });

        _mockConnectionManager.Setup(c => c.HasConnection("s1")).Returns(true);
        _mockConnectionManager.Setup(c => c.SendAsync("s1", It.IsAny<AdviceWebSocketMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var engine = new WorkflowEngine(new[] { mockWorkflow.Object }, _mockConnectionManager.Object, _mockDispatcher.Object, _mockLogger.Object);

        await engine.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "test_workflow",
            NuzlockeId = "s1"
        });

        _mockConnectionManager.Verify(c => c.SendAsync("s1",
            It.Is<WorkflowEventMessage>(m =>
                m.Type == "workflow_event" &&
                !string.IsNullOrEmpty(m.CorrelationId)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

/// <summary>
/// Tests for WorkflowBase template method pattern using a concrete test implementation
/// </summary>
public class WorkflowBaseTests
{
    private readonly Mock<IStateManager> _mockState = new();
    private readonly Mock<IPokeApiConnector> _mockPokeApi = new();
    private readonly Mock<IAiProvider> _mockAi = new();
    private readonly Mock<ILogger> _mockLogger = new();

    public WorkflowBaseTests()
    {
        _mockState.Setup(s => s.GetStateAsync(It.IsAny<string>()))
            .ReturnsAsync(new NuzlockeState());
        _mockState.Setup(s => s.GetBattleContextAsync(It.IsAny<string>()))
            .ReturnsAsync(new BattleContext());
    }

    [Fact]
    public async Task Execute_ValidationFails_ReturnsErrors()
    {
        var workflow = new FailingValidationWorkflow(_mockState.Object, _mockPokeApi.Object, _mockAi.Object, _mockLogger.Object);

        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "test",
            NuzlockeId = "s1",
            Parameters = new WorkflowParameters()
        });

        Assert.False(result.Success);
        Assert.Contains("Missing required parameter: species", result.Errors);
    }

    [Fact]
    public async Task Execute_FullPipeline_CallsAllSteps()
    {
        _mockAi.Setup(a => a.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test advice");

        var workflow = new TrackingWorkflow(_mockState.Object, _mockPokeApi.Object, _mockAi.Object, _mockLogger.Object);

        var json = """{"species": "pikachu"}""";
        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(json)!;

        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "tracking",
            NuzlockeId = "s1",
            Parameters = new WorkflowParameters(dict)
        });

        Assert.True(result.Success);
        Assert.True(workflow.FetchDataCalled);
        Assert.True(workflow.MutateStateCalled);
        Assert.Equal("Test advice", result.Advice);
    }

    [Fact]
    public async Task Execute_ExceptionInMutate_ReturnsFailure()
    {
        var workflow = new ThrowingMutateWorkflow(_mockState.Object, _mockPokeApi.Object, _mockAi.Object, _mockLogger.Object);

        var json = """{"species": "pikachu"}""";
        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(json)!;

        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "throwing",
            NuzlockeId = "s1",
            Parameters = new WorkflowParameters(dict)
        });

        Assert.False(result.Success);
        Assert.Contains("Workflow execution failed", result.Errors[0]);
    }

    // --- Test workflow implementations ---

    private class FailingValidationWorkflow : WorkflowBase
    {
        public override string WorkflowId => "failing_validation";
        public FailingValidationWorkflow(IStateManager sm, IPokeApiConnector pa, IAiProvider ai, ILogger l)
            : base(sm, pa, ai, l) { }

        public override IReadOnlyList<string> Validate(WorkflowParameters p) =>
            ValidateRequired(p, "species", "nickname");

        protected override string GetSystemPrompt(WorkflowContext ctx) => "test";
        protected override string BuildUserMessage(WorkflowContext ctx) => "test";
    }

    private class TrackingWorkflow : WorkflowBase
    {
        public bool FetchDataCalled { get; private set; }
        public bool MutateStateCalled { get; private set; }

        public override string WorkflowId => "tracking";
        public TrackingWorkflow(IStateManager sm, IPokeApiConnector pa, IAiProvider ai, ILogger l)
            : base(sm, pa, ai, l) { }

        public override IReadOnlyList<string> Validate(WorkflowParameters p) =>
            ValidateRequired(p, "species");

        protected override Task FetchDataAsync(WorkflowContext ctx, CancellationToken ct)
        {
            FetchDataCalled = true;
            return Task.CompletedTask;
        }

        protected override Task MutateStateAsync(WorkflowContext ctx, CancellationToken ct)
        {
            MutateStateCalled = true;
            ctx.Result.Mutations.Add(new StateMutation { Type = "test", Description = "test mutation" });
            return Task.CompletedTask;
        }

        protected override string GetSystemPrompt(WorkflowContext ctx) => "You are a test assistant.";
        protected override string BuildUserMessage(WorkflowContext ctx) => "Test message";
    }

    private class ThrowingMutateWorkflow : WorkflowBase
    {
        public override string WorkflowId => "throwing";
        public ThrowingMutateWorkflow(IStateManager sm, IPokeApiConnector pa, IAiProvider ai, ILogger l)
            : base(sm, pa, ai, l) { }

        public override IReadOnlyList<string> Validate(WorkflowParameters p) =>
            ValidateRequired(p, "species");

        protected override Task MutateStateAsync(WorkflowContext ctx, CancellationToken ct) =>
            throw new InvalidOperationException("Database error");

        protected override string GetSystemPrompt(WorkflowContext ctx) => "test";
        protected override string BuildUserMessage(WorkflowContext ctx) => "test";
    }
}
