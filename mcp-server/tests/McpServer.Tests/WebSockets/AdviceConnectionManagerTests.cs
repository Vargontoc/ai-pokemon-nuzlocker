using System.Net.WebSockets;
using es.vargontoc.nuzlocke.ai.WebSockets;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests.WebSockets;

public class AdviceConnectionManagerTests
{
    private readonly AdviceConnectionManager _manager;

    public AdviceConnectionManagerTests()
    {
        _manager = new AdviceConnectionManager(
            new Mock<ILogger<AdviceConnectionManager>>().Object);
    }

    [Fact]
    public void HasConnection_NoConnection_ReturnsFalse()
    {
        Assert.False(_manager.HasConnection("session1"));
    }

    [Fact]
    public void AddConnection_ThenHasConnection_ReturnsTrue()
    {
        var socket = CreateOpenWebSocket();
        _manager.AddConnection("session1", socket);

        Assert.True(_manager.HasConnection("session1"));
    }

    [Fact]
    public void RemoveConnection_ThenHasConnection_ReturnsFalse()
    {
        var socket = CreateOpenWebSocket();
        _manager.AddConnection("session1", socket);
        _manager.RemoveConnection("session1");

        Assert.False(_manager.HasConnection("session1"));
    }

    [Fact]
    public void RemoveConnection_NonExistent_DoesNotThrow()
    {
        _manager.RemoveConnection("nonexistent");
    }

    [Fact]
    public void AddConnection_ReplacesExisting()
    {
        var socket1 = CreateOpenWebSocket();
        var socket2 = CreateOpenWebSocket();

        _manager.AddConnection("session1", socket1);
        _manager.AddConnection("session1", socket2);

        Assert.True(_manager.HasConnection("session1"));
    }

    [Fact]
    public async Task SendAsync_NoConnection_ReturnsFalse()
    {
        var result = await _manager.SendAsync("session1", new AdviceStartMessage
        {
            Type = "advice_start",
            CorrelationId = "abc",
            WorkflowId = "test"
        });

        Assert.False(result);
    }

    [Fact]
    public void HasConnection_ClosedSocket_ReturnsFalse()
    {
        var socket = CreateClosedWebSocket();
        _manager.AddConnection("session1", socket);

        Assert.False(_manager.HasConnection("session1"));
    }

    [Fact]
    public async Task SendAsync_ClosedSocket_ReturnsFalseAndRemoves()
    {
        var socket = CreateClosedWebSocket();
        _manager.AddConnection("session1", socket);

        var result = await _manager.SendAsync("session1", new AdviceStartMessage
        {
            Type = "advice_start",
            CorrelationId = "abc",
            WorkflowId = "test"
        });

        Assert.False(result);
        Assert.False(_manager.HasConnection("session1"));
    }

    private static WebSocket CreateOpenWebSocket()
    {
        var mock = new Mock<WebSocket>();
        mock.Setup(w => w.State).Returns(WebSocketState.Open);
        mock.Setup(w => w.SendAsync(
                It.IsAny<ArraySegment<byte>>(),
                It.IsAny<WebSocketMessageType>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock.Object;
    }

    private static WebSocket CreateClosedWebSocket()
    {
        var mock = new Mock<WebSocket>();
        mock.Setup(w => w.State).Returns(WebSocketState.Closed);
        return mock.Object;
    }
}
