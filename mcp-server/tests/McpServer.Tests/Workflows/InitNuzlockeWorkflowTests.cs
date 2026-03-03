using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;
using es.vargontoc.nuzlocke.ai.Workflows;
using es.vargontoc.nuzlocke.ai.Workflows.Setup;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests.Workflows;

public class InitNuzlockeWorkflowTests
{
    private readonly Mock<IStateManager> _mockState = new();
    private readonly Mock<IPokeApiConnector> _mockPokeApi = new();
    private readonly Mock<IAiProvider> _mockAi = new();
    private readonly Mock<INuzlockeRepository> _mockRepository = new();
    private readonly Mock<ILogger<InitNuzlockeWorkflow>> _mockLogger = new();

    private InitNuzlockeWorkflow CreateWorkflow() =>
        new(_mockState.Object, _mockPokeApi.Object, _mockAi.Object,
            _mockRepository.Object, _mockLogger.Object);

    private static NuzlockeMetadata MakeMetadata(string id, bool isInitialized = false, int generation = 1, LockeType lockeType = LockeType.Standard) =>
        new()
        {
            Id = id,
            Name = "Test Run",
            Generation = generation,
            LockeType = lockeType,
            IsInitialized = isInitialized
        };

    // ─── Validate always returns empty ────────────────────────────────────────

    [Fact]
    public void Validate_AnyParameters_ReturnsNoErrors()
    {
        var workflow = CreateWorkflow();
        var errors = workflow.Validate(new WorkflowParameters(new Dictionary<string, System.Text.Json.JsonElement>()));
        Assert.Empty(errors);
    }

    // ─── Nuzlocke not found ────────────────────────────────────────────────────

    [Fact]
    public async Task Execute_NuzlockeNotFound_ReturnsFailure()
    {
        var nuzlockeId = Guid.NewGuid().ToString();
        _mockRepository.Setup(r => r.GetMetadataAsync(nuzlockeId))
            .ReturnsAsync((NuzlockeMetadata?)null);

        var workflow = CreateWorkflow();

        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "init_nuzlocke",
            NuzlockeId = nuzlockeId
        });

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains(nuzlockeId));
    }

    // ─── Not yet initialized ──────────────────────────────────────────────────

    [Fact]
    public async Task Execute_NotInitialized_SavesGameStateAndSetsIsInitialized()
    {
        var nuzlockeId = Guid.NewGuid().ToString();
        var metadata = MakeMetadata(nuzlockeId, isInitialized: false, generation: 1, lockeType: LockeType.Hardcore);

        _mockRepository.Setup(r => r.GetMetadataAsync(nuzlockeId)).ReturnsAsync(metadata);
        _mockRepository.Setup(r => r.GetGameStateAsync(nuzlockeId)).ReturnsAsync(new NuzlockeState());
        _mockRepository.Setup(r => r.GetBattleStateAsync(nuzlockeId)).ReturnsAsync((BattleContext?)null);
        _mockRepository.Setup(r => r.SaveGameStateAsync(nuzlockeId, It.IsAny<NuzlockeState>())).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveMetadataAsync(It.IsAny<NuzlockeMetadata>())).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.UpdateStatusAsync(nuzlockeId, NuzlockeStatus.Active)).ReturnsAsync(true);

        var workflow = CreateWorkflow();

        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "init_nuzlocke",
            NuzlockeId = nuzlockeId
        });

        Assert.True(result.Success);
        Assert.Single(result.Mutations);
        Assert.Equal("nuzlocke_initialized", result.Mutations[0].Type);
        Assert.Equal(1, result.Data["generation"]);
        Assert.Equal("Hardcore", result.Data["locke_type"]);

        _mockRepository.Verify(r => r.SaveMetadataAsync(
            It.Is<NuzlockeMetadata>(m => m.IsInitialized)), Times.Once);
        _mockRepository.Verify(r => r.SaveGameStateAsync(nuzlockeId, It.IsAny<NuzlockeState>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateStatusAsync(nuzlockeId, NuzlockeStatus.Active), Times.Once);
    }

    [Fact]
    public async Task Execute_NotInitialized_DoesNotCallAi()
    {
        var nuzlockeId = Guid.NewGuid().ToString();
        var metadata = MakeMetadata(nuzlockeId, isInitialized: false);

        _mockRepository.Setup(r => r.GetMetadataAsync(nuzlockeId)).ReturnsAsync(metadata);
        _mockRepository.Setup(r => r.GetGameStateAsync(nuzlockeId)).ReturnsAsync(new NuzlockeState());
        _mockRepository.Setup(r => r.GetBattleStateAsync(nuzlockeId)).ReturnsAsync((BattleContext?)null);
        _mockRepository.Setup(r => r.SaveGameStateAsync(nuzlockeId, It.IsAny<NuzlockeState>())).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveMetadataAsync(It.IsAny<NuzlockeMetadata>())).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.UpdateStatusAsync(nuzlockeId, NuzlockeStatus.Active)).ReturnsAsync(true);

        var workflow = CreateWorkflow();

        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "init_nuzlocke",
            NuzlockeId = nuzlockeId
        });

        Assert.Null(result.Advice);
        _mockAi.Verify(a => a.GetCompletionAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Already initialized ──────────────────────────────────────────────────

    [Fact]
    public async Task Execute_AlreadyInitialized_AddsWarningMutation()
    {
        var nuzlockeId = Guid.NewGuid().ToString();
        var metadata = MakeMetadata(nuzlockeId, isInitialized: true);

        _mockRepository.Setup(r => r.GetMetadataAsync(nuzlockeId)).ReturnsAsync(metadata);
        _mockRepository.Setup(r => r.GetGameStateAsync(nuzlockeId)).ReturnsAsync(new NuzlockeState());
        _mockRepository.Setup(r => r.GetBattleStateAsync(nuzlockeId)).ReturnsAsync((BattleContext?)null);

        var workflow = CreateWorkflow();

        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "init_nuzlocke",
            NuzlockeId = nuzlockeId
        });

        Assert.True(result.Success);
        Assert.Single(result.Mutations);
        Assert.Equal("nuzlocke_already_initialized", result.Mutations[0].Type);
    }

    [Fact]
    public async Task Execute_AlreadyInitialized_SkipsAdviceAndSave()
    {
        var nuzlockeId = Guid.NewGuid().ToString();
        var metadata = MakeMetadata(nuzlockeId, isInitialized: true);

        _mockRepository.Setup(r => r.GetMetadataAsync(nuzlockeId)).ReturnsAsync(metadata);
        _mockRepository.Setup(r => r.GetGameStateAsync(nuzlockeId)).ReturnsAsync(new NuzlockeState());
        _mockRepository.Setup(r => r.GetBattleStateAsync(nuzlockeId)).ReturnsAsync((BattleContext?)null);

        var workflow = CreateWorkflow();

        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "init_nuzlocke",
            NuzlockeId = nuzlockeId
        });

        Assert.Null(result.Advice);
        _mockAi.Verify(a => a.GetCompletionAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.SaveMetadataAsync(It.IsAny<NuzlockeMetadata>()), Times.Never);
        _mockRepository.Verify(r => r.SaveGameStateAsync(It.IsAny<string>(), It.IsAny<NuzlockeState>()), Times.Never);
    }
}
