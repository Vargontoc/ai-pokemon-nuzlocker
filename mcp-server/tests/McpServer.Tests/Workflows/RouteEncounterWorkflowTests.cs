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

public class RouteEncounterWorkflowTests
{
    private readonly Mock<IStateManager> _mockState = new();
    private readonly Mock<IPokeApiConnector> _mockPokeApi = new();
    private readonly Mock<IAiProvider> _mockAi = new();
    private readonly Mock<INuzlockeRepository> _mockRepository = new();
    private readonly Mock<ILogger<RouteEncounterWorkflow>> _mockLogger = new();

    private NuzlockeState _currentState = new() { Generation = 1, LockeType = "standard" };

    public RouteEncounterWorkflowTests()
    {
        _mockRepository.Setup(f => f.GetNuzlockePathAsync("test_nuzlocke_2026-02-15"))
            .ReturnsAsync("/tmp/nuzlockes/test_nuzlocke_2026-02-15");

        _mockState.Setup(s => s.GetStateAsync(It.IsAny<string>()))
            .ReturnsAsync(() => _currentState);
        _mockState.Setup(s => s.GetBattleContextAsync(It.IsAny<string>()))
            .ReturnsAsync(new BattleContext());

        _mockPokeApi.Setup(p => p.GetPokemonSubsetAsync("pidgey", It.IsAny<int>()))
            .ReturnsAsync(new PokemonSubset
            {
                Id = 16, Name = "pidgey",
                Types = new List<string> { "normal", "flying" },
                Stats = new Dictionary<string, int> { { "hp", 40 }, { "attack", 45 }, { "defense", 40 } },
                MovesBasicos = new List<string> { "tackle", "sand-attack", "gust" }
            });

        _mockPokeApi.Setup(p => p.GetPokemonSubsetAsync("rattata", It.IsAny<int>()))
            .ReturnsAsync(new PokemonSubset
            {
                Id = 19, Name = "rattata",
                Types = new List<string> { "normal" },
                Stats = new Dictionary<string, int> { { "hp", 30 }, { "attack", 56 }, { "defense", 35 } },
                MovesBasicos = new List<string> { "tackle", "tail-whip", "quick-attack" }
            });

        _mockPokeApi.Setup(p => p.GetPokemonSubsetAsync("pikachu", It.IsAny<int>()))
            .ReturnsAsync(new PokemonSubset
            {
                Id = 25, Name = "pikachu",
                Types = new List<string> { "electric" },
                Stats = new Dictionary<string, int> { { "hp", 35 }, { "attack", 55 }, { "defense", 40 } },
                MovesBasicos = new List<string> { "thunder-shock", "growl", "tail-whip" }
            });

        _mockAi.Setup(a => a.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("I recommend catching Pidgey for the flying type coverage!");
    }

    private RouteEncounterWorkflow CreateWorkflow() =>
        new(_mockState.Object, _mockPokeApi.Object, _mockAi.Object, _mockRepository.Object, _mockLogger.Object);

    private static WorkflowParameters MakeParams(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
        return new WorkflowParameters(dict);
    }

    private static WorkflowParameters DefaultParams() => MakeParams(new
    {
        nuzlocke_id = "test_nuzlocke_2026-02-15",
        route_name = "Route 1",
        available_pokemon = new[] { "pidgey", "rattata" }
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
    public void Validate_MissingNuzlockeId_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var errors = workflow.Validate(MakeParams(new { route_name = "Route 1" }));
        Assert.Contains(errors, e => e.Contains("nuzlocke_id"));
    }

    [Fact]
    public void Validate_MissingRouteName_ReturnsError()
    {
        var workflow = CreateWorkflow();
        var errors = workflow.Validate(MakeParams(new { nuzlocke_id = "test" }));
        Assert.Contains(errors, e => e.Contains("route_name"));
    }

    // --- BuildContextAsync: nuzlocke not found ---

    [Fact]
    public async Task Execute_NuzlockeNotFound_ReturnsFailure()
    {
        _mockRepository.Setup(f => f.GetNuzlockePathAsync(It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        var workflow = CreateWorkflow();
        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "route_encounter",
            NuzlockeId = "s1",
            Parameters = DefaultParams()
        });

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Nuzlocke not found"));
    }

    // --- BuildContextAsync: route already has encounter ---

    [Fact]
    public async Task Execute_RouteAlreadyHasEncounter_ReturnsFailure()
    {
        _currentState = new NuzlockeState
        {
            Generation = 1,
            LockeType = "standard",
            Encounters = new Dictionary<string, EncounterRecord>
            {
                ["Route 1"] = new EncounterRecord
                {
                    Location = "Route 1",
                    CapturedSpecies = "pidgey",
                    CapturedNickname = "Birdie",
                    EncounterUsed = true
                }
            }
        };

        var workflow = CreateWorkflow();
        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "route_encounter",
            NuzlockeId = "s1",
            Parameters = DefaultParams()
        });

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("already has a recorded encounter"));
    }

    // --- FetchData ---

    [Fact]
    public async Task Execute_FetchData_CallsPokeApiForEachAvailablePokemon()
    {
        var workflow = CreateWorkflow();
        await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "route_encounter",
            NuzlockeId = "s1",
            Parameters = DefaultParams()
        });

        _mockPokeApi.Verify(p => p.GetPokemonSubsetAsync("pidgey", It.IsAny<int>()), Times.Once);
        _mockPokeApi.Verify(p => p.GetPokemonSubsetAsync("rattata", It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task Execute_FetchData_EmptyAvailablePokemon_StillSucceeds()
    {
        var workflow = CreateWorkflow();
        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "route_encounter",
            NuzlockeId = "s1",
            Parameters = MakeParams(new
            {
                nuzlocke_id = "test_nuzlocke_2026-02-15",
                route_name = "Route 1"
                // no available_pokemon
            })
        });

        Assert.True(result.Success);
        _mockPokeApi.Verify(p => p.GetPokemonSubsetAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Execute_FetchData_PokemonNotFound_SkipsWithoutFailing()
    {
        _mockPokeApi.Setup(p => p.GetPokemonSubsetAsync("unknown_mon", It.IsAny<int>()))
            .ThrowsAsync(new HttpRequestException("Not found"));

        var workflow = CreateWorkflow();
        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "route_encounter",
            NuzlockeId = "s1",
            Parameters = MakeParams(new
            {
                nuzlocke_id = "test_nuzlocke_2026-02-15",
                route_name = "Route 1",
                available_pokemon = new[] { "pidgey", "unknown_mon" }
            })
        });

        Assert.True(result.Success);
        // Only pidgey should be in the result
        var available = result.Data["available_pokemon"] as List<PokemonSubset>;
        Assert.NotNull(available);
        Assert.Single(available!);
        Assert.Equal("pidgey", available![0].Name);
    }

    // --- MutateState ---

    [Fact]
    public async Task Execute_NoStateMutations()
    {
        var workflow = CreateWorkflow();
        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "route_encounter",
            NuzlockeId = "s1",
            Parameters = DefaultParams()
        });

        Assert.True(result.Success);
        Assert.Empty(result.Mutations);
        _mockState.Verify(s => s.SaveStateAsync(It.IsAny<string>(), It.IsAny<NuzlockeState>()), Times.Never);
        _mockState.Verify(s => s.RecordEncounterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // --- GenerateAdvice ---

    [Fact]
    public async Task Execute_GeneratesAdvice()
    {
        var workflow = CreateWorkflow();
        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "route_encounter",
            NuzlockeId = "s1",
            Parameters = DefaultParams()
        });

        Assert.Equal("I recommend catching Pidgey for the flying type coverage!", result.Advice);
    }

    [Fact]
    public async Task Execute_AdvicePromptContainsTeamAndAvailablePokemon()
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
            },
            PCStorage = new List<StoredPokemon>
            {
                new() { Nickname = "Catty", Species = "caterpie", Level = 3, Moves = new List<string> { "tackle", "string-shot" } }
            }
        };

        _mockAi.Setup(a => a.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((sys, usr, _) => { capturedSystem = sys; capturedUser = usr; })
            .ReturnsAsync("Catch pidgey!");

        var workflow = CreateWorkflow();
        await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "route_encounter",
            NuzlockeId = "s1",
            Parameters = DefaultParams()
        });

        Assert.NotNull(capturedSystem);
        Assert.Contains("Generation 1", capturedSystem!);
        Assert.Contains("one capture per route", capturedSystem!);

        Assert.NotNull(capturedUser);
        Assert.Contains("Route 1", capturedUser!);
        Assert.Contains("pidgey", capturedUser!);
        Assert.Contains("rattata", capturedUser!);
        Assert.Contains("Blaze", capturedUser!); // team
        Assert.Contains("Catty", capturedUser!); // PC
    }

    // --- Result.Data ---

    [Fact]
    public async Task Execute_ResultDataContainsAvailablePokemonAndRouteName()
    {
        var workflow = CreateWorkflow();
        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "route_encounter",
            NuzlockeId = "s1",
            Parameters = DefaultParams()
        });

        Assert.True(result.Success);
        Assert.True(result.Data.ContainsKey("available_pokemon"));
        Assert.True(result.Data.ContainsKey("route_name"));
        Assert.Equal("Route 1", result.Data["route_name"]);

        var available = result.Data["available_pokemon"] as List<PokemonSubset>;
        Assert.NotNull(available);
        Assert.Equal(2, available!.Count);
        Assert.Contains(available, p => p.Name == "pidgey");
        Assert.Contains(available, p => p.Name == "rattata");
    }

    // --- Validation fails → no API calls ---

    [Fact]
    public async Task Execute_ValidationFails_NoApiCalls()
    {
        var workflow = CreateWorkflow();
        var result = await workflow.ExecuteAsync(new WorkflowRequest
        {
            WorkflowId = "route_encounter",
            NuzlockeId = "s1",
            Parameters = MakeParams(new { }) // empty
        });

        Assert.False(result.Success);
        _mockPokeApi.Verify(p => p.GetPokemonSubsetAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    // --- ExecuteDeterministicAsync ---

    [Fact]
    public async Task ExecuteDeterministic_ReturnsPromptsWithoutCallingLLM()
    {
        var workflow = CreateWorkflow();
        var result = await workflow.ExecuteDeterministicAsync(new WorkflowRequest
        {
            WorkflowId = "route_encounter",
            NuzlockeId = "s1",
            Parameters = DefaultParams()
        });

        Assert.True(result.Result.Success);
        Assert.NotNull(result.SystemPrompt);
        Assert.NotNull(result.UserMessage);
        Assert.Contains("one capture per route", result.SystemPrompt!);
        Assert.Contains("pidgey", result.UserMessage!);

        _mockAi.Verify(a => a.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
