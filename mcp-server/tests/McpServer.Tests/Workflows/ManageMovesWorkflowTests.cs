using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;
using es.vargontoc.nuzlocke.ai.Workflows;
using es.vargontoc.nuzlocke.ai.Workflows.Gameplay;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests.Workflows;

public class ManageMovesWorkflowTests
{
    private readonly Mock<IStateManager> _mockState = new();
    private readonly Mock<IPokeApiConnector> _mockPokeApi = new();
    private readonly Mock<IAiProvider> _mockAi = new();
    private readonly Mock<INuzlockeFileManager> _mockFileManager = new();
    private readonly Mock<ILogger<ManageMovesWorkflow>> _mockLogger = new();

    private NuzlockeState _state = new() { Generation = 1, LockeType = "standard" };

    public ManageMovesWorkflowTests()
    {
        _mockFileManager.Setup(f => f.GetNuzlockePathAsync("nuzlocke-1"))
            .ReturnsAsync("/tmp/nuzlocke-1");

        _mockState.Setup(s => s.GetStateAsync("nuzlocke-1"))
            .ReturnsAsync(() => _state);
        _mockState.Setup(s => s.GetBattleContextAsync("nuzlocke-1"))
            .ReturnsAsync(new BattleContext());
        _mockState.Setup(s => s.SaveStateAsync("nuzlocke-1", It.IsAny<NuzlockeState>()))
            .Returns(Task.CompletedTask);

        _mockPokeApi.Setup(p => p.GetMoveAsync("surf"))
            .ReturnsAsync(new MoveData
            {
                Name = "surf",
                Accuracy = 100,
                Power = 90,
                Pp = 15,
                Type = new NamedApiResource { Name = "water" },
                DamageClass = new NamedApiResource { Name = "special" },
                EffectEntries = new List<MoveEffectEntry>
                {
                    new() { Language = new NamedApiResource { Name = "en" }, ShortEffect = "Hits all adjacent Pokemon." }
                }
            });

        _mockAi.Setup(a => a.GetCompletionAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Surf is an excellent move for coverage!");
    }

    private ManageMovesWorkflow CreateWorkflow() =>
        new(_mockState.Object, _mockPokeApi.Object, _mockAi.Object,
            _mockFileManager.Object, _mockLogger.Object);

    private static WorkflowParameters MakeParams(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
        return new WorkflowParameters(dict);
    }

    private static WorkflowRequest MakeRequest(object parameters) =>
        new WorkflowRequest
        {
            WorkflowId = "manage_moves",
            SessionId = "nuzlocke-1",
            Language = "en-US",
            Parameters = MakeParams(parameters)
        };

    // -------------------------------------------------------------------------
    // Vertiente A
    // -------------------------------------------------------------------------

    [Fact]
    public async Task VertienteA_TeamPokemon_OverwritesMoves()
    {
        _state.Team.Add(new TeamMember
        {
            Nickname = "Sparky",
            Species = "pikachu",
            Moves = new List<string> { "thunder-shock" }
        });

        var result = await CreateWorkflow().ExecuteAsync(MakeRequest(new
        {
            nuzlocke_id = "nuzlocke-1",
            nickname = "Sparky",
            moves = new[] { "thunderbolt", "quick-attack", "thunder-wave", "slam" }
        }));

        Assert.True(result.Success);
        Assert.Equal(new[] { "thunderbolt", "quick-attack", "thunder-wave", "slam" },
            _state.Team[0].Moves);
        Assert.Single(result.Mutations);
        Assert.Equal("moves_updated", result.Mutations[0].Type);
        // No LLM advice for Vertiente A
        Assert.Null(result.Advice);
    }

    [Fact]
    public async Task VertienteA_PCPokemon_OverwritesMoves()
    {
        _state.PCStorage.Add(new StoredPokemon
        {
            Nickname = "Boxed",
            Species = "rattata",
            Moves = new List<string> { "tackle" }
        });

        var result = await CreateWorkflow().ExecuteAsync(MakeRequest(new
        {
            nuzlocke_id = "nuzlocke-1",
            nickname = "Boxed",
            moves = new[] { "tackle", "quick-attack" }
        }));

        Assert.True(result.Success);
        Assert.Equal(new[] { "tackle", "quick-attack" }, _state.PCStorage[0].Moves);
    }

    [Fact]
    public async Task VertienteA_MovesExceedFour_ReturnsFail()
    {
        _state.Team.Add(new TeamMember { Nickname = "Sparky", Species = "pikachu" });

        var result = await CreateWorkflow().ExecuteAsync(MakeRequest(new
        {
            nuzlocke_id = "nuzlocke-1",
            nickname = "Sparky",
            moves = new[] { "a", "b", "c", "d", "e" }
        }));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("4"));
    }

    // -------------------------------------------------------------------------
    // Vertiente B
    // -------------------------------------------------------------------------

    [Fact]
    public async Task VertienteB_LearnMove_FetchesPokeApiAndMutatesState()
    {
        _state.Team.Add(new TeamMember
        {
            Nickname = "Sparky",
            Species = "pikachu",
            Moves = new List<string> { "thunderbolt", "quick-attack", "thunder-wave" }
        });

        var result = await CreateWorkflow().ExecuteAsync(MakeRequest(new
        {
            nuzlocke_id = "nuzlocke-1",
            nickname = "Sparky",
            learn_move = "surf",
            learn_source = "HM"
        }));

        Assert.True(result.Success);
        Assert.Contains("surf", _state.Team[0].Moves);
        Assert.Single(result.Mutations);
        Assert.Equal("move_learned", result.Mutations[0].Type);
        _mockPokeApi.Verify(p => p.GetMoveAsync("surf"), Times.Once);
    }

    [Fact]
    public async Task VertienteB_ForgetMove_ReplacesCorrectSlot()
    {
        _state.Team.Add(new TeamMember
        {
            Nickname = "Sparky",
            Species = "pikachu",
            Moves = new List<string> { "thunderbolt", "quick-attack", "thunder-wave", "slam" }
        });

        _mockPokeApi.Setup(p => p.GetMoveAsync("slam"))
            .ReturnsAsync(new MoveData
            {
                Name = "slam",
                Type = new NamedApiResource { Name = "normal" },
                DamageClass = new NamedApiResource { Name = "physical" },
                Pp = 20,
                EffectEntries = new List<MoveEffectEntry>()
            });

        var result = await CreateWorkflow().ExecuteAsync(MakeRequest(new
        {
            nuzlocke_id = "nuzlocke-1",
            nickname = "Sparky",
            learn_move = "surf",
            forget_move = "slam",
            learn_source = "HM"
        }));

        Assert.True(result.Success);
        var moves = _state.Team[0].Moves;
        Assert.Contains("surf", moves);
        Assert.DoesNotContain("slam", moves);
        Assert.Equal(4, moves.Count);
        Assert.Contains("slam", result.Mutations[0].Description);
        Assert.Contains("surf", result.Mutations[0].Description);
    }

    [Fact]
    public async Task VertienteB_GeneratesLlmAdvice()
    {
        _state.Team.Add(new TeamMember
        {
            Nickname = "Sparky",
            Species = "pikachu",
            Moves = new List<string> { "thunderbolt", "quick-attack" }
        });

        var result = await CreateWorkflow().ExecuteAsync(MakeRequest(new
        {
            nuzlocke_id = "nuzlocke-1",
            nickname = "Sparky",
            learn_move = "surf",
            learn_source = "HM"
        }));

        Assert.True(result.Success);
        Assert.NotNull(result.Advice);
        Assert.Contains("Surf", result.Advice!, StringComparison.OrdinalIgnoreCase);
        _mockAi.Verify(a => a.GetCompletionAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
