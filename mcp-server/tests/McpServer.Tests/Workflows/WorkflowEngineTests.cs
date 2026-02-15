using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;
using es.vargontoc.nuzlocke.ai.Workflows;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests.Workflows;

public class WorkflowEngineTests
{
    private readonly Mock<ILogger<WorkflowEngine>> _mockLogger = new();

    [Fact]
    public async Task Execute_UnknownWorkflow_ReturnsFailure()
    {
        var engine = new WorkflowEngine(Array.Empty<IWorkflow>(), _mockLogger.Object);

        var result = await engine.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "nonexistent",
            SessionId = "s1"
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

        var engine = new WorkflowEngine(new[] { mockWorkflow.Object }, _mockLogger.Object);

        var result = await engine.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "test_workflow",
            SessionId = "s1"
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

        var engine = new WorkflowEngine(new[] { mockWorkflow.Object }, _mockLogger.Object);

        var result = await engine.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "CAPTURE_POKEMON",
            SessionId = "s1"
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

        var engine = new WorkflowEngine(new[] { w1.Object, w2.Object }, _mockLogger.Object);

        var available = engine.GetAvailableWorkflows();
        Assert.Equal(2, available.Count);
        Assert.Contains("workflow_a", available);
        Assert.Contains("workflow_b", available);
    }

    [Fact]
    public void GetAvailableWorkflows_EmptyWhenNoWorkflows()
    {
        var engine = new WorkflowEngine(Array.Empty<IWorkflow>(), _mockLogger.Object);
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

        var engine = new WorkflowEngine(new[] { w1.Object, w2.Object }, _mockLogger.Object);

        var r1 = await engine.ExecuteAsync(new WorkflowRequest { WorkflowId = "alpha", SessionId = "s1" });
        var r2 = await engine.ExecuteAsync(new WorkflowRequest { WorkflowId = "beta", SessionId = "s1" });

        Assert.Equal("Alpha advice", r1.Advice);
        Assert.Equal("Beta advice", r2.Advice);
        w1.Verify(w => w.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        w2.Verify(w => w.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()), Times.Once);
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
            SessionId = "s1",
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
            SessionId = "s1",
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
            SessionId = "s1",
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
