using es.vargontoc.nuzlocke.ai.Controllers;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Services;
using es.vargontoc.nuzlocke.ai.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests.Controllers;

public class NuzlockeControllerTests
{
    private readonly Mock<INuzlockeRepository> _mockRepository = new();
    private readonly NuzlockeController _controller;

    public NuzlockeControllerTests()
    {
        _controller = new NuzlockeController(new Mock<ILogger<NuzlockeController>>().Object);
    }

    private static NuzlockeMetadata MakeMetadata(string id, bool isInitialized) =>
        new() { Id = id, Name = "Test Run", IsInitialized = isInitialized };

    private NuzlockeController CreateControllerWithContext(out DefaultHttpContext httpContext)
    {
        httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        var controller = new NuzlockeController(new Mock<ILogger<NuzlockeController>>().Object);
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    // ─── GET /nuzlocke/{id}/party ─────────────────────────────────────────────

    [Fact]
    public async Task GetParty_NuzlockeNotFound_Returns404()
    {
        _mockRepository.Setup(r => r.GetMetadataAsync("missing")).ReturnsAsync((NuzlockeMetadata?)null);

        var result = await _controller.GetParty("missing", _mockRepository.Object);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetParty_NotInitialized_Returns409()
    {
        var id = Guid.NewGuid().ToString();
        _mockRepository.Setup(r => r.GetMetadataAsync(id)).ReturnsAsync(MakeMetadata(id, isInitialized: false));

        var result = await _controller.GetParty(id, _mockRepository.Object);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task GetParty_Initialized_ReturnsTeam()
    {
        var id = Guid.NewGuid().ToString();
        var state = new NuzlockeState
        {
            Team = new List<TeamMember>
            {
                new() { Nickname = "Charizard", Species = "charizard", Level = 50 }
            }
        };

        _mockRepository.Setup(r => r.GetMetadataAsync(id)).ReturnsAsync(MakeMetadata(id, isInitialized: true));
        _mockRepository.Setup(r => r.GetGameStateAsync(id)).ReturnsAsync(state);

        var result = await _controller.GetParty(id, _mockRepository.Object);

        var ok = Assert.IsType<OkObjectResult>(result);
        var party = Assert.IsType<List<TeamMember>>(ok.Value);
        Assert.Single(party);
        Assert.Equal("Charizard", party[0].Nickname);
    }

    // ─── GET /nuzlocke/{id}/pc ────────────────────────────────────────────────

    [Fact]
    public async Task GetPc_NuzlockeNotFound_Returns404()
    {
        _mockRepository.Setup(r => r.GetMetadataAsync("missing")).ReturnsAsync((NuzlockeMetadata?)null);

        var result = await _controller.GetPc("missing", _mockRepository.Object);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetPc_NotInitialized_Returns409()
    {
        var id = Guid.NewGuid().ToString();
        _mockRepository.Setup(r => r.GetMetadataAsync(id)).ReturnsAsync(MakeMetadata(id, isInitialized: false));

        var result = await _controller.GetPc(id, _mockRepository.Object);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task GetPc_Initialized_ReturnsPCStorage()
    {
        var id = Guid.NewGuid().ToString();
        var state = new NuzlockeState
        {
            PCStorage = new List<StoredPokemon>
            {
                new() { Nickname = "Blasty", Species = "blastoise", Level = 40 }
            }
        };

        _mockRepository.Setup(r => r.GetMetadataAsync(id)).ReturnsAsync(MakeMetadata(id, isInitialized: true));
        _mockRepository.Setup(r => r.GetGameStateAsync(id)).ReturnsAsync(state);

        var result = await _controller.GetPc(id, _mockRepository.Object);

        var ok = Assert.IsType<OkObjectResult>(result);
        var pc = Assert.IsType<List<StoredPokemon>>(ok.Value);
        Assert.Single(pc);
        Assert.Equal("Blasty", pc[0].Nickname);
    }

    // ─── GET /nuzlocke/{id}/inventory ─────────────────────────────────────────

    [Fact]
    public async Task GetInventory_NuzlockeNotFound_Returns404()
    {
        _mockRepository.Setup(r => r.GetMetadataAsync("missing")).ReturnsAsync((NuzlockeMetadata?)null);

        var result = await _controller.GetInventory("missing", _mockRepository.Object);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetInventory_NotInitialized_Returns409()
    {
        var id = Guid.NewGuid().ToString();
        _mockRepository.Setup(r => r.GetMetadataAsync(id)).ReturnsAsync(MakeMetadata(id, isInitialized: false));

        var result = await _controller.GetInventory(id, _mockRepository.Object);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task GetInventory_Initialized_ReturnsInventory()
    {
        var id = Guid.NewGuid().ToString();
        var state = new NuzlockeState
        {
            Inventory = new List<InventoryItem>
            {
                new() { Name = "potion", Quantity = 3 }
            }
        };

        _mockRepository.Setup(r => r.GetMetadataAsync(id)).ReturnsAsync(MakeMetadata(id, isInitialized: true));
        _mockRepository.Setup(r => r.GetGameStateAsync(id)).ReturnsAsync(state);

        var result = await _controller.GetInventory(id, _mockRepository.Object);

        var ok = Assert.IsType<OkObjectResult>(result);
        var inventory = Assert.IsType<List<InventoryItem>>(ok.Value);
        Assert.Single(inventory);
        Assert.Equal("potion", inventory[0].Name);
        Assert.Equal(3, inventory[0].Quantity);
    }

    // ─── GET /nuzlocke/{id}/graveyard ─────────────────────────────────────────

    [Fact]
    public async Task GetGraveyard_NuzlockeNotFound_Returns404()
    {
        _mockRepository.Setup(r => r.GetMetadataAsync("missing")).ReturnsAsync((NuzlockeMetadata?)null);

        var result = await _controller.GetGraveyard("missing", _mockRepository.Object);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetGraveyard_NotInitialized_Returns409()
    {
        var id = Guid.NewGuid().ToString();
        _mockRepository.Setup(r => r.GetMetadataAsync(id)).ReturnsAsync(MakeMetadata(id, isInitialized: false));

        var result = await _controller.GetGraveyard(id, _mockRepository.Object);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task GetGraveyard_Initialized_ReturnsDeadPokemon()
    {
        var id = Guid.NewGuid().ToString();
        var state = new NuzlockeState
        {
            DeadPokemon = new List<DeadPokemon>
            {
                new() { Nickname = "Pidgey Jr.", Species = "pidgeot", Level = 30, CauseOfDeath = "critical hit" }
            }
        };

        _mockRepository.Setup(r => r.GetMetadataAsync(id)).ReturnsAsync(MakeMetadata(id, isInitialized: true));
        _mockRepository.Setup(r => r.GetGameStateAsync(id)).ReturnsAsync(state);

        var result = await _controller.GetGraveyard(id, _mockRepository.Object);

        var ok = Assert.IsType<OkObjectResult>(result);
        var graveyard = Assert.IsType<List<DeadPokemon>>(ok.Value);
        Assert.Single(graveyard);
        Assert.Equal("Pidgey Jr.", graveyard[0].Nickname);
        Assert.Equal("critical hit", graveyard[0].CauseOfDeath);
    }

    // ─── POST /nuzlocke/advice ────────────────────────────────────────────────

    [Fact]
    public async Task GetAdvice_NuzlockeNotFound_Returns404()
    {
        var id = Guid.NewGuid().ToString();
        _mockRepository.Setup(r => r.GetMetadataAsync(id)).ReturnsAsync((NuzlockeMetadata?)null);

        var result = await _controller.GetAdvice(
            new AdviceRequest("question", id),
            new Mock<IAdviceDispatcher>().Object,
            _mockRepository.Object,
            new Mock<IAdviceConnectionManager>().Object);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetAdvice_NotInitialized_Returns409()
    {
        var id = Guid.NewGuid().ToString();
        _mockRepository.Setup(r => r.GetMetadataAsync(id)).ReturnsAsync(MakeMetadata(id, isInitialized: false));

        var result = await _controller.GetAdvice(
            new AdviceRequest("question", id),
            new Mock<IAdviceDispatcher>().Object,
            _mockRepository.Object,
            new Mock<IAdviceConnectionManager>().Object);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task GetAdvice_NoWebSocketConnection_Returns503()
    {
        var id = Guid.NewGuid().ToString();
        _mockRepository.Setup(r => r.GetMetadataAsync(id)).ReturnsAsync(MakeMetadata(id, isInitialized: true));
        var mockConnections = new Mock<IAdviceConnectionManager>();
        mockConnections.Setup(c => c.HasConnection(id)).Returns(false);

        var result = await _controller.GetAdvice(
            new AdviceRequest("question", id),
            new Mock<IAdviceDispatcher>().Object,
            _mockRepository.Object,
            mockConnections.Object);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetAdvice_Valid_Returns202AndDispatchesAdvice()
    {
        var id = Guid.NewGuid().ToString();
        _mockRepository.Setup(r => r.GetMetadataAsync(id)).ReturnsAsync(MakeMetadata(id, isInitialized: true));
        var mockConnections = new Mock<IAdviceConnectionManager>();
        mockConnections.Setup(c => c.HasConnection(id)).Returns(true);
        var mockDispatcher = new Mock<IAdviceDispatcher>();

        var result = await _controller.GetAdvice(
            new AdviceRequest("Should I use Pikachu?", id),
            mockDispatcher.Object,
            _mockRepository.Object,
            mockConnections.Object);

        var accepted = Assert.IsType<AcceptedResult>(result);
        Assert.Equal(StatusCodes.Status202Accepted, accepted.StatusCode);
        mockDispatcher.Verify(d => d.DispatchAgentAdvice(
            It.Is<AgentAdviceDispatchRequest>(r => r.NuzlockeId == id && r.Question == "Should I use Pikachu?")),
            Times.Once);
    }

    // ─── POST /nuzlocke/advice/stream ─────────────────────────────────────────

    [Fact]
    public async Task StreamAdvice_NuzlockeNotFound_Returns404()
    {
        var controller = CreateControllerWithContext(out var httpContext);
        var id = Guid.NewGuid().ToString();
        _mockRepository.Setup(r => r.GetMetadataAsync(id)).ReturnsAsync((NuzlockeMetadata?)null);

        await controller.StreamAdvice(
            new AdviceRequest("question", id),
            null!,
            _mockRepository.Object,
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task StreamAdvice_NotInitialized_Returns409()
    {
        var controller = CreateControllerWithContext(out var httpContext);
        var id = Guid.NewGuid().ToString();
        _mockRepository.Setup(r => r.GetMetadataAsync(id)).ReturnsAsync(MakeMetadata(id, isInitialized: false));

        await controller.StreamAdvice(
            new AdviceRequest("question", id),
            null!,
            _mockRepository.Object,
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
    }
}
