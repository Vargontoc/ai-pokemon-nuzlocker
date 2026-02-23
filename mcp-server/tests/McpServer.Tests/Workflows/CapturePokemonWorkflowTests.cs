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

public class CapturePokemonWorkflowTests
{
    private readonly Mock<IStateManager> _mockState = new();
    private readonly Mock<IPokeApiConnector> _mockPokeApi = new();
    private readonly Mock<IAiProvider> _mockAi = new();
    private readonly Mock<INuzlockeFileManager> _mockFileManager = new();
    private readonly Mock<ILogger<CapturePokemonWorkflow>> _mockLogger = new();

    private NuzlockeState _currentState = new() { Generation = 1, LockeType = "standard" };

    public CapturePokemonWorkflowTests()
    {
        // Default: nuzlocke_id is known (L1 cache + L2 SQLite)
        _mockFileManager.Setup(f => f.GetNuzlockePathAsync("test_nuzlocke_2026-02-15"))
            .ReturnsAsync("/tmp/nuzlockes/test_nuzlocke_2026-02-15");

        // State manager delegates (returns current state)
        _mockState.Setup(s => s.GetStateAsync(It.IsAny<string>()))
            .ReturnsAsync(() => _currentState);
        _mockState.Setup(s => s.GetBattleContextAsync(It.IsAny<string>()))
            .ReturnsAsync(new BattleContext());

        // Default: encounter recording succeeds
        _mockState.Setup(s => s.RecordEncounterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Default: add to team succeeds
        _mockState.Setup(s => s.AddToTeamAsync(It.IsAny<string>(), It.IsAny<TeamMember>()))
            .ReturnsAsync(true);

        // Default: save state succeeds
        _mockState.Setup(s => s.SaveStateAsync(It.IsAny<string>(), It.IsAny<NuzlockeState>()))
            .Returns(Task.CompletedTask);

        // Default: PokeAPI returns pikachu data
        _mockPokeApi.Setup(p => p.GetPokemonSubsetAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new PokemonSubset
            {
                Id = 25,
                Name = "pikachu",
                Types = new List<string> { "electric" },
                Stats = new Dictionary<string, int> { { "hp", 35 }, { "attack", 55 }, { "defense", 40 } },
                MovesBasicos = new List<string> { "thunder-shock", "growl", "tail-whip" }
            });

        // Default: AI advice
        _mockAi.Setup(a => a.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Pikachu is a great addition to your team!");
    }

    private readonly Mock<IStatsCalculator> _mockStatsCalc = new();

    private CapturePokemonWorkflow CreateWorkflow()
    {
        _mockStatsCalc.Setup(c => c.Calculate(
            It.IsAny<int[]>(), It.IsAny<int[]>(), It.IsAny<int[]>(), It.IsAny<int>(), It.IsAny<float>()))
            .Returns(new PokemonStats { HP = 45, Attack = 55, Defense = 40, Speed = 90, Special = 50 });
        return new(_mockState.Object, _mockPokeApi.Object, _mockAi.Object, _mockFileManager.Object, _mockStatsCalc.Object, _mockLogger.Object);
    }

    private static WorkflowParameters MakeParams(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
        return new WorkflowParameters(dict);
    }

    private static WorkflowParameters DefaultParams() => MakeParams(new
    {
        nuzlocke_id = "test_nuzlocke_2026-02-15",
        species = "pikachu",
        nickname = "Sparky",
        location = "Viridian Forest",
        level = 5
    });

    // --- Validation ---

    [Fact]
    public void Validate_AllParamsPresent_ReturnsNoErrors()
    {
        var workflow = CreateWorkflow();
        var errors = workflow.Validate(DefaultParams());
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_MissingSpecies_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var errors = workflow.Validate(MakeParams(new
        {
            nuzlocke_id = "test", nickname = "Sparky", location = "Route 1", level = 5
        }));
        Assert.Contains(errors, e => e.Contains("species"));
    }

    [Fact]
    public void Validate_MissingNickname_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var errors = workflow.Validate(MakeParams(new
        {
            nuzlocke_id = "test", species = "pikachu", location = "Route 1", level = 5
        }));
        Assert.Contains(errors, e => e.Contains("nickname"));
    }

    [Fact]
    public void Validate_MissingLocation_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var errors = workflow.Validate(MakeParams(new
        {
            nuzlocke_id = "test", species = "pikachu", nickname = "Sparky", level = 5
        }));
        Assert.Contains(errors, e => e.Contains("location"));
    }

    [Fact]
    public void Validate_MissingLevel_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var errors = workflow.Validate(MakeParams(new
        {
            nuzlocke_id = "test", species = "pikachu", nickname = "Sparky", location = "Route 1"
        }));
        Assert.Contains(errors, e => e.Contains("level"));
    }

    [Fact]
    public void Validate_MissingNuzlockeId_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var errors = workflow.Validate(MakeParams(new
        {
            species = "pikachu", nickname = "Sparky", location = "Route 1", level = 5
        }));
        Assert.Contains(errors, e => e.Contains("nuzlocke_id"));
    }

    // --- FetchData ---

    [Fact]
    public async Task Execute_FetchData_CallsPokeApiWithSpecies()
    {
        var workflow = CreateWorkflow();

        await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "capture_pokemon",
            SessionId = "s1",
            Parameters = DefaultParams()
        });

        _mockPokeApi.Verify(p => p.GetPokemonSubsetAsync("pikachu", It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task Execute_FetchData_PokemonNotFound_ReturnsFailure()
    {
        _mockPokeApi.Setup(p => p.GetPokemonSubsetAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((PokemonSubset?)null);

        var workflow = CreateWorkflow();

        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "capture_pokemon",
            SessionId = "s1",
            Parameters = DefaultParams()
        });

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Pokemon not found"));
    }

    // --- MutateState: add to team ---

    [Fact]
    public async Task Execute_TeamNotFull_AddsToTeam()
    {
        _currentState = new NuzlockeState { Generation = 1, LockeType = "standard" }; // empty team

        var workflow = CreateWorkflow();

        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "capture_pokemon",
            SessionId = "s1",
            Parameters = DefaultParams()
        });

        Assert.True(result.Success);
        Assert.Equal("team", result.Data["destination"]);
        Assert.Contains(result.Mutations, m => m.Type == "encounter_recorded");
        Assert.Contains(result.Mutations, m => m.Type == "added_to_team");
        _mockState.Verify(s => s.AddToTeamAsync("test_nuzlocke_2026-02-15", It.Is<TeamMember>(t =>
            t.Nickname == "Sparky" && t.Species == "pikachu" && t.Level == 5)), Times.Once);
    }

    // --- MutateState: team full → PC ---

    [Fact]
    public async Task Execute_TeamFull_SendsToPC()
    {
        _currentState = new NuzlockeState
        {
            Generation = 1,
            LockeType = "standard",
            Team = Enumerable.Range(1, 6).Select(i => new TeamMember
            {
                Nickname = $"Mon{i}", Species = $"species{i}", Level = i * 10
            }).ToList()
        };

        var workflow = CreateWorkflow();

        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "capture_pokemon",
            SessionId = "s1",
            Parameters = DefaultParams()
        });

        Assert.True(result.Success);
        Assert.Equal("pc", result.Data["destination"]);
        Assert.Contains(result.Mutations, m => m.Type == "encounter_recorded");
        Assert.Contains(result.Mutations, m => m.Type == "added_to_pc");
        // Should NOT call AddToTeamAsync
        _mockState.Verify(s => s.AddToTeamAsync(It.IsAny<string>(), It.IsAny<TeamMember>()), Times.Never);
        // Should call SaveStateAsync (PC storage updated directly)
        _mockState.Verify(s => s.SaveStateAsync(It.IsAny<string>(), It.IsAny<NuzlockeState>()), Times.Once);
    }

    // --- MutateState: duplicate location ---

    [Fact]
    public async Task Execute_DuplicateLocation_ReturnsFailure()
    {
        _mockState.Setup(s => s.RecordEncounterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false); // encounter already recorded

        var workflow = CreateWorkflow();

        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "capture_pokemon",
            SessionId = "s1",
            Parameters = DefaultParams()
        });

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Encounter already recorded"));
    }

    // --- GenerateAdvice ---

    [Fact]
    public async Task Execute_GeneratesAdvice()
    {
        var workflow = CreateWorkflow();

        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "capture_pokemon",
            SessionId = "s1",
            Parameters = DefaultParams()
        });

        Assert.Equal("Pikachu is a great addition to your team!", result.Advice);
    }

    [Fact]
    public async Task Execute_AdvicePromptContainsTeamAndPokemonData()
    {
        string? capturedSystem = null;
        string? capturedUser = null;

        _currentState = new NuzlockeState
        {
            Generation = 1,
            LockeType = "standard",
            Team = new List<TeamMember>
            {
                new() { Nickname = "Blaze", Species = "charmander", Level = 12, Moves = new List<string> { "scratch", "ember" } }
            }
        };

        _mockAi.Setup(a => a.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((sys, usr, _) => { capturedSystem = sys; capturedUser = usr; })
            .ReturnsAsync("Good capture!");

        var workflow = CreateWorkflow();

        await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "capture_pokemon",
            SessionId = "s1",
            Parameters = DefaultParams()
        });

        Assert.NotNull(capturedSystem);
        Assert.Contains("Generation 1", capturedSystem!);
        Assert.NotNull(capturedUser);
        Assert.Contains("pikachu", capturedUser!);
        Assert.Contains("electric", capturedUser!);
        Assert.Contains("Sparky", capturedUser!);
    }

    // --- Result.Data ---

    [Fact]
    public async Task Execute_ResultDataContainsPokemonInfoAndDestination()
    {
        var workflow = CreateWorkflow();

        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "capture_pokemon",
            SessionId = "s1",
            Parameters = DefaultParams()
        });

        Assert.True(result.Success);
        Assert.True(result.Data.ContainsKey("pokemon"));
        Assert.True(result.Data.ContainsKey("destination"));

        var pokemon = result.Data["pokemon"] as PokemonSubset;
        Assert.NotNull(pokemon);
        Assert.Equal("pikachu", pokemon!.Name);
    }

    // --- Nuzlocke not found ---

    [Fact]
    public async Task Execute_NuzlockeNotFound_ReturnsFailure()
    {
        _mockFileManager.Setup(f => f.GetNuzlockePathAsync(It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        var workflow = CreateWorkflow();

        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "capture_pokemon",
            SessionId = "s1",
            Parameters = DefaultParams()
        });

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Nuzlocke not found"));
    }

    // --- Validation fails → no API calls ---

    [Fact]
    public async Task Execute_ValidationFails_NoApiCalls()
    {
        var workflow = CreateWorkflow();

        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "capture_pokemon",
            SessionId = "s1",
            Parameters = MakeParams(new { }) // empty params
        });

        Assert.False(result.Success);
        _mockPokeApi.Verify(p => p.GetPokemonSubsetAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _mockState.Verify(s => s.RecordEncounterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
