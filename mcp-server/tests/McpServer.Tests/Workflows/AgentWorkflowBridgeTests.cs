using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Services;
using es.vargontoc.nuzlocke.ai.Workflows;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests.Workflows;

public class AgentWorkflowBridgeTests
{
    private readonly Mock<IStateManager> _mockState = new();
    private readonly Mock<IPokeApiConnector> _mockPokeApi = new();
    private readonly Mock<IWorkflowEngine> _mockEngine = new();
    private readonly Mock<ILogger<ToolExecutor>> _mockLogger = new();
    private readonly ToolExecutor _executor;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public AgentWorkflowBridgeTests()
    {
        _executor = new ToolExecutor(
            _mockState.Object,
            _mockPokeApi.Object,
            _mockEngine.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteWorkflow_CallsWorkflowEngine()
    {
        // Arrange
        _mockEngine.Setup(e => e.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowResult { WorkflowId = "capture_pokemon", Success = true });

        var toolCall = new ToolCall
        {
            Id = "call_1",
            Name = "execute_workflow",
            ArgumentsJson = JsonSerializer.Serialize(new
            {
                workflowId = "capture_pokemon",
                parameters = JsonSerializer.Serialize(new
                {
                    nuzlocke_id = "abc123",
                    species = "pikachu",
                    nickname = "Sparky",
                    location = "Viridian Forest",
                    level = 5
                })
            }, _jsonOptions)
        };

        // Act
        var result = await _executor.ExecuteAsync(toolCall);

        // Assert
        _mockEngine.Verify(e => e.ExecuteAsync(
            It.Is<WorkflowRequest>(r =>
                r.WorkflowId == "capture_pokemon" &&
                r.Parameters.GetString("species") == "pikachu" &&
                r.Parameters.GetString("nickname") == "Sparky" &&
                r.Parameters.GetInt("level") == 5),
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.Contains("capture_pokemon", result.Content);
        Assert.Contains("true", result.Content.ToLower());
    }

    [Fact]
    public async Task ExecuteWorkflow_MapsParametersCorrectly()
    {
        // Arrange
        WorkflowRequest? capturedRequest = null;
        _mockEngine.Setup(e => e.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowRequest, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(new WorkflowResult { WorkflowId = "item_obtained", Success = true });

        var toolCall = new ToolCall
        {
            Id = "call_2",
            Name = "execute_workflow",
            ArgumentsJson = JsonSerializer.Serialize(new
            {
                workflowId = "item_obtained",
                parameters = JsonSerializer.Serialize(new
                {
                    nuzlocke_id = "abc123",
                    item_name = "potion",
                    quantity = 3,
                    category = "potion",
                    location = "Viridian City"
                })
            }, _jsonOptions)
        };

        // Act
        await _executor.ExecuteAsync(toolCall);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("item_obtained", capturedRequest!.WorkflowId);
        Assert.Equal("abc123", capturedRequest.SessionId);
        Assert.Equal("potion", capturedRequest.Parameters.GetString("item_name"));
        Assert.Equal(3, capturedRequest.Parameters.GetInt("quantity"));
        Assert.Equal("potion", capturedRequest.Parameters.GetString("category"));
        Assert.Equal("Viridian City", capturedRequest.Parameters.GetString("location"));
    }

    [Fact]
    public async Task ExecuteWorkflow_InvalidWorkflowId_ReturnsError()
    {
        // Arrange
        _mockEngine.Setup(e => e.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowResult.Failure("unknown_workflow", "Unknown workflow: 'unknown_workflow'. Available: init_nuzlocke, capture_pokemon"));

        var toolCall = new ToolCall
        {
            Id = "call_3",
            Name = "execute_workflow",
            ArgumentsJson = JsonSerializer.Serialize(new
            {
                workflowId = "unknown_workflow",
                parameters = "{}"
            }, _jsonOptions)
        };

        // Act
        var result = await _executor.ExecuteAsync(toolCall);

        // Assert
        Assert.Contains("unknown_workflow", result.Content, StringComparison.OrdinalIgnoreCase);
        var parsed = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.False(parsed.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task ExecuteWorkflow_InvalidParameters_ReturnsWorkflowErrors()
    {
        // Arrange — workflow returns validation errors
        _mockEngine.Setup(e => e.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WorkflowResult.Failure("capture_pokemon",
                "Missing required parameter: species",
                "Missing required parameter: nickname"));

        var toolCall = new ToolCall
        {
            Id = "call_4",
            Name = "execute_workflow",
            ArgumentsJson = JsonSerializer.Serialize(new
            {
                workflowId = "capture_pokemon",
                parameters = JsonSerializer.Serialize(new { nuzlocke_id = "abc123" })
            }, _jsonOptions)
        };

        // Act
        var result = await _executor.ExecuteAsync(toolCall);

        // Assert
        Assert.Contains("species", result.Content);
        Assert.Contains("nickname", result.Content);
    }

    [Fact]
    public async Task ExecuteWorkflow_SessionIdFromNuzlockeId()
    {
        // Arrange
        WorkflowRequest? capturedRequest = null;
        _mockEngine.Setup(e => e.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowRequest, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(new WorkflowResult { WorkflowId = "capture_pokemon", Success = true });

        var toolCall = new ToolCall
        {
            Id = "call_5",
            Name = "execute_workflow",
            ArgumentsJson = JsonSerializer.Serialize(new
            {
                workflowId = "capture_pokemon",
                parameters = JsonSerializer.Serialize(new
                {
                    nuzlocke_id = "my_nuzlocke_123",
                    species = "weedle",
                    nickname = "Bug",
                    location = "Route 2",
                    level = 3
                })
            }, _jsonOptions)
        };

        // Act
        await _executor.ExecuteAsync(toolCall);

        // Assert — sessionId should be extracted from nuzlocke_id in parameters
        Assert.NotNull(capturedRequest);
        Assert.Equal("my_nuzlocke_123", capturedRequest!.SessionId);
    }

    [Fact]
    public async Task ExecuteWorkflow_InvalidParametersJson_ReturnsJsonError()
    {
        var toolCall = new ToolCall
        {
            Id = "call_6",
            Name = "execute_workflow",
            ArgumentsJson = JsonSerializer.Serialize(new
            {
                workflowId = "capture_pokemon",
                parameters = "this is not valid json {{"
            }, _jsonOptions)
        };

        // Act
        var result = await _executor.ExecuteAsync(toolCall);

        // Assert
        Assert.Contains("Invalid parameters JSON", result.Content);
    }

    [Fact]
    public async Task ExecuteWorkflow_MissingWorkflowId_ThrowsError()
    {
        var toolCall = new ToolCall
        {
            Id = "call_7",
            Name = "execute_workflow",
            ArgumentsJson = JsonSerializer.Serialize(new
            {
                parameters = "{}"
            }, _jsonOptions)
        };

        // Act
        var result = await _executor.ExecuteAsync(toolCall);

        // Assert — should return error (caught by ToolExecutor's global catch)
        Assert.Contains("error", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteWorkflow_EmptyParameters_DefaultsToEmptyObject()
    {
        // Arrange
        WorkflowRequest? capturedRequest = null;
        _mockEngine.Setup(e => e.ExecuteAsync(It.IsAny<WorkflowRequest>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowRequest, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(new WorkflowResult { WorkflowId = "init_nuzlocke", Success = true });

        var toolCall = new ToolCall
        {
            Id = "call_8",
            Name = "execute_workflow",
            ArgumentsJson = JsonSerializer.Serialize(new
            {
                workflowId = "init_nuzlocke"
                // no parameters field
            }, _jsonOptions)
        };

        // Act
        await _executor.ExecuteAsync(toolCall);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("init_nuzlocke", capturedRequest!.WorkflowId);
    }
}
