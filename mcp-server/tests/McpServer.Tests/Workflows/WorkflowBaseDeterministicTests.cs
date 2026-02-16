using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;
using es.vargontoc.nuzlocke.ai.Workflows;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests.Workflows;

public class WorkflowBaseDeterministicTests
{
    private readonly Mock<IStateManager> _mockState = new();
    private readonly Mock<IPokeApiConnector> _mockPokeApi = new();
    private readonly Mock<IAiProvider> _mockAi = new();
    private readonly Mock<ILogger> _mockLogger = new();

    public WorkflowBaseDeterministicTests()
    {
        _mockState.Setup(s => s.GetStateAsync(It.IsAny<string>()))
            .ReturnsAsync(new NuzlockeState());
        _mockState.Setup(s => s.GetBattleContextAsync(It.IsAny<string>()))
            .ReturnsAsync(new BattleContext());
    }

    [Fact]
    public async Task ExecuteDeterministic_ValidationFails_ReturnsFailure()
    {
        var workflow = new TestWorkflow(_mockState.Object, _mockPokeApi.Object, _mockAi.Object, _mockLogger.Object);

        var result = await workflow.ExecuteDeterministicAsync(new WorkflowRequest
        {
            WorkflowId = "test",
            SessionId = "s1",
            Parameters = new WorkflowParameters() // missing "name"
        });

        Assert.False(result.Result.Success);
        Assert.Contains("Missing required parameter: name", result.Result.Errors);
        Assert.Null(result.SystemPrompt);
        Assert.Null(result.UserMessage);
    }

    [Fact]
    public async Task ExecuteDeterministic_Success_ReturnsPromptsWithoutCallingLLM()
    {
        var workflow = new TestWorkflow(_mockState.Object, _mockPokeApi.Object, _mockAi.Object, _mockLogger.Object);

        var json = """{"name": "pikachu"}""";
        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(json)!;

        var result = await workflow.ExecuteDeterministicAsync(new WorkflowRequest
        {
            WorkflowId = "test",
            SessionId = "s1",
            Parameters = new WorkflowParameters(dict)
        });

        Assert.True(result.Result.Success);
        Assert.Equal("You are a test assistant.", result.SystemPrompt);
        Assert.Equal("Analyze pikachu", result.UserMessage);

        // LLM should NOT have been called
        _mockAi.Verify(
            a => a.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockAi.Verify(
            a => a.StreamCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteDeterministic_RunsFetchAndMutate()
    {
        var workflow = new TestWorkflow(_mockState.Object, _mockPokeApi.Object, _mockAi.Object, _mockLogger.Object);

        var json = """{"name": "pikachu"}""";
        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(json)!;

        var result = await workflow.ExecuteDeterministicAsync(new WorkflowRequest
        {
            WorkflowId = "test",
            SessionId = "s1",
            Parameters = new WorkflowParameters(dict)
        });

        Assert.True(workflow.FetchCalled);
        Assert.True(workflow.MutateCalled);
        Assert.Single(result.Result.Mutations);
        Assert.Equal("test_mutation", result.Result.Mutations[0].Type);
    }

    [Fact]
    public async Task ExecuteDeterministic_MutateThrows_ReturnsFailure()
    {
        var workflow = new ThrowingWorkflow(_mockState.Object, _mockPokeApi.Object, _mockAi.Object, _mockLogger.Object);

        var json = """{"name": "pikachu"}""";
        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(json)!;

        var result = await workflow.ExecuteDeterministicAsync(new WorkflowRequest
        {
            WorkflowId = "test",
            SessionId = "s1",
            Parameters = new WorkflowParameters(dict)
        });

        Assert.False(result.Result.Success);
        Assert.Contains("Workflow execution failed", result.Result.Errors[0]);
        Assert.Null(result.SystemPrompt);
        Assert.Null(result.UserMessage);
    }

    [Fact]
    public async Task ExecuteAsync_StillWorks_WithBuildContextAsync()
    {
        _mockAi.Setup(a => a.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Great advice!");

        var workflow = new TestWorkflow(_mockState.Object, _mockPokeApi.Object, _mockAi.Object, _mockLogger.Object);

        var json = """{"name": "pikachu"}""";
        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(json)!;

        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "test",
            SessionId = "s1",
            Parameters = new WorkflowParameters(dict)
        });

        Assert.True(result.Success);
        Assert.Equal("Great advice!", result.Advice);
        Assert.True(workflow.FetchCalled);
        Assert.True(workflow.MutateCalled);
    }

    // --- Test workflow implementations ---

    private class TestWorkflow : WorkflowBase
    {
        public bool FetchCalled { get; private set; }
        public bool MutateCalled { get; private set; }

        public override string WorkflowId => "test";

        public TestWorkflow(IStateManager sm, IPokeApiConnector pa, IAiProvider ai, ILogger l)
            : base(sm, pa, ai, l) { }

        public override IReadOnlyList<string> Validate(WorkflowParameters p) =>
            ValidateRequired(p, "name");

        protected override Task FetchDataAsync(WorkflowContext ctx, CancellationToken ct)
        {
            FetchCalled = true;
            return Task.CompletedTask;
        }

        protected override Task MutateStateAsync(WorkflowContext ctx, CancellationToken ct)
        {
            MutateCalled = true;
            ctx.Result.Mutations.Add(new StateMutation { Type = "test_mutation", Description = "test" });
            return Task.CompletedTask;
        }

        protected override string GetSystemPrompt(WorkflowContext ctx) => "You are a test assistant.";
        protected override string BuildUserMessage(WorkflowContext ctx) =>
            $"Analyze {ctx.Parameters.GetString("name")}";
    }

    private class ThrowingWorkflow : WorkflowBase
    {
        public override string WorkflowId => "throwing";

        public ThrowingWorkflow(IStateManager sm, IPokeApiConnector pa, IAiProvider ai, ILogger l)
            : base(sm, pa, ai, l) { }

        public override IReadOnlyList<string> Validate(WorkflowParameters p) =>
            ValidateRequired(p, "name");

        protected override Task MutateStateAsync(WorkflowContext ctx, CancellationToken ct) =>
            throw new InvalidOperationException("DB error");

        protected override string GetSystemPrompt(WorkflowContext ctx) => "test";
        protected override string BuildUserMessage(WorkflowContext ctx) => "test";
    }
}
