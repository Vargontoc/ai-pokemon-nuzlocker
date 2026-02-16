using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;
using es.vargontoc.nuzlocke.ai.Workflows;
using es.vargontoc.nuzlocke.ai.Workflows.Setup;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests.Workflows;

public class InitNuzlockeWorkflowTests
{
    private readonly Mock<IStateManager> _mockState = new();
    private readonly Mock<IPokeApiConnector> _mockPokeApi = new();
    private readonly Mock<IAiProvider> _mockAi = new();
    private readonly Mock<INuzlockeFileManager> _mockFileManager = new();
    private readonly Mock<ILogger<InitNuzlockeWorkflow>> _mockLogger = new();

    public InitNuzlockeWorkflowTests()
    {
        _mockState.Setup(s => s.GetStateAsync(It.IsAny<string>()))
            .ReturnsAsync(new NuzlockeState());
        _mockState.Setup(s => s.GetBattleContextAsync(It.IsAny<string>()))
            .ReturnsAsync(new BattleContext());
    }

    private InitNuzlockeWorkflow CreateWorkflow() =>
        new(_mockState.Object, _mockPokeApi.Object, _mockAi.Object, _mockFileManager.Object, _mockLogger.Object);

    private static WorkflowParameters MakeParams(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
        return new WorkflowParameters(dict);
    }

    // --- Validation ---

    [Fact]
    public void Validate_MissingBasePath_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var errors = workflow.Validate(MakeParams(new { generation = 1 }));
        Assert.Contains(errors, e => e.Contains("base_path"));
    }

    [Fact]
    public void Validate_MissingGeneration_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var errors = workflow.Validate(MakeParams(new { base_path = "/tmp/test" }));
        Assert.Contains(errors, e => e.Contains("generation"));
    }

    [Fact]
    public void Validate_GenerationNot1_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var errors = workflow.Validate(MakeParams(new { base_path = "/tmp/test", generation = 2 }));
        Assert.Contains(errors, e => e.Contains("Only generation 1"));
    }

    [Fact]
    public void Validate_ValidParams_ReturnsNoErrors()
    {
        var workflow = CreateWorkflow();
        var errors = workflow.Validate(MakeParams(new { base_path = "/tmp/test", generation = 1 }));
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ValidParamsWithLockeType_ReturnsNoErrors()
    {
        var workflow = CreateWorkflow();
        var errors = workflow.Validate(MakeParams(new { base_path = "/tmp/test", generation = 1, locke_type = "hardcore" }));
        Assert.Empty(errors);
    }

    // --- Execution ---

    [Fact]
    public async Task Execute_CreatesNuzlockeViaFileManager()
    {
        var expectedId = "abc12345_2026-02-15";
        _mockFileManager.Setup(f => f.CreateNuzlockeAsync(It.IsAny<string>(), 1, "standard"))
            .ReturnsAsync(expectedId);
        _mockAi.Setup(a => a.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Pick Squirtle for Brock.");

        var workflow = CreateWorkflow();

        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "init_nuzlocke",
            SessionId = "s1",
            Parameters = MakeParams(new { base_path = "/tmp/nuzlockes", generation = 1 })
        });

        Assert.True(result.Success);
        Assert.Equal(expectedId, result.Data["nuzlocke_id"]);
        Assert.Equal(1, result.Data["generation"]);
        Assert.Equal("standard", result.Data["locke_type"]);
        Assert.Single(result.Mutations);
        Assert.Equal("nuzlocke_created", result.Mutations[0].Type);
        _mockFileManager.Verify(f => f.CreateNuzlockeAsync("/tmp/nuzlockes", 1, "standard"), Times.Once);
    }

    [Fact]
    public async Task Execute_GeneratesAdvice()
    {
        _mockFileManager.Setup(f => f.CreateNuzlockeAsync(It.IsAny<string>(), 1, "standard"))
            .ReturnsAsync("abc12345_2026-02-15");
        _mockAi.Setup(a => a.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Pick Squirtle for an easier time against Brock.");

        var workflow = CreateWorkflow();

        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "init_nuzlocke",
            SessionId = "s1",
            Parameters = MakeParams(new { base_path = "/tmp/nuzlockes", generation = 1 })
        });

        Assert.Equal("Pick Squirtle for an easier time against Brock.", result.Advice);
    }

    [Fact]
    public async Task Execute_AdvicePromptContainsGenerationAndLockeType()
    {
        string? capturedSystem = null;
        string? capturedUser = null;

        _mockFileManager.Setup(f => f.CreateNuzlockeAsync(It.IsAny<string>(), 1, "hardcore"))
            .ReturnsAsync("abc12345_2026-02-15");
        _mockAi.Setup(a => a.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((sys, usr, _) => { capturedSystem = sys; capturedUser = usr; })
            .ReturnsAsync("Advice");

        var workflow = CreateWorkflow();

        await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "init_nuzlocke",
            SessionId = "s1",
            Parameters = MakeParams(new { base_path = "/tmp/nuzlockes", generation = 1, locke_type = "hardcore" })
        });

        Assert.NotNull(capturedSystem);
        Assert.Contains("Generation 1", capturedSystem!);
        Assert.Contains("hardcore", capturedSystem!);
        Assert.NotNull(capturedUser);
        Assert.Contains("Generation 1", capturedUser!);
        Assert.Contains("hardcore", capturedUser!);
    }

    [Fact]
    public async Task Execute_ValidationFails_ReturnsFailure()
    {
        var workflow = CreateWorkflow();

        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "init_nuzlocke",
            SessionId = "s1",
            Parameters = MakeParams(new { }) // missing required params
        });

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        _mockFileManager.Verify(f => f.CreateNuzlockeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Execute_DefaultLockeType_IsStandard()
    {
        _mockFileManager.Setup(f => f.CreateNuzlockeAsync(It.IsAny<string>(), 1, "standard"))
            .ReturnsAsync("abc12345_2026-02-15");
        _mockAi.Setup(a => a.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Advice");

        var workflow = CreateWorkflow();

        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "init_nuzlocke",
            SessionId = "s1",
            Parameters = MakeParams(new { base_path = "/tmp/nuzlockes", generation = 1 })
        });

        Assert.True(result.Success);
        Assert.Equal("standard", result.Data["locke_type"]);
        _mockFileManager.Verify(f => f.CreateNuzlockeAsync(It.IsAny<string>(), 1, "standard"), Times.Once);
    }
}
