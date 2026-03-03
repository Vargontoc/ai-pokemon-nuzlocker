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

public class EvolutionWorkflowTests
{
    private readonly Mock<IStateManager> _mockState = new();
    private readonly Mock<IPokeApiConnector> _mockPokeApi = new();
    private readonly Mock<IAiProvider> _mockAi = new();
    private readonly Mock<INuzlockeRepository> _mockRepository = new();
    private readonly Mock<IStatsCalculator> _mockCalc = new();
    private readonly Mock<ILogger<EvolutionWorkflow>> _mockLogger = new();

    private NuzlockeState _state = new() { Generation = 1, LockeType = "standard" };

    private static readonly PokemonStats RaichuStats = new()
    { HP = 120, Attack = 110, Defense = 85, Speed = 130, Special = 110 };

    public EvolutionWorkflowTests()
    {
        _mockRepository.Setup(f => f.GetNuzlockePathAsync("nuzlocke-1"))
            .ReturnsAsync("/tmp/nuzlocke-1");

        _mockState.Setup(s => s.GetStateAsync("nuzlocke-1"))
            .ReturnsAsync(() => _state);
        _mockState.Setup(s => s.GetBattleContextAsync("nuzlocke-1"))
            .ReturnsAsync(new BattleContext());
        _mockState.Setup(s => s.SaveStateAsync("nuzlocke-1", It.IsAny<NuzlockeState>()))
            .Returns(Task.CompletedTask);

        // Default: PokeAPI returns Raichu data
        _mockPokeApi.Setup(p => p.GetPokemonSubsetAsync("raichu", It.IsAny<int>()))
            .ReturnsAsync(new PokemonSubset
            {
                Name = "raichu",
                Types = new List<string> { "electric" },
                Stats = new Dictionary<string, int>
                {
                    { "hp", 60 }, { "attack", 90 }, { "defense", 55 },
                    { "speed", 110 }, { "special-attack", 90 }
                }
            });

        _mockCalc.Setup(c => c.Calculate(
            It.IsAny<int[]>(), It.IsAny<int[]>(), It.IsAny<int[]>(), It.IsAny<int>(), It.IsAny<float>()))
            .Returns(RaichuStats);

        _mockAi.Setup(a => a.GetCompletionAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Raichu is a significant improvement! Higher Speed and Attack.");
    }

    private EvolutionWorkflow CreateWorkflow() =>
        new(_mockState.Object, _mockPokeApi.Object, _mockAi.Object,
            _mockRepository.Object, _mockCalc.Object, _mockLogger.Object);

    private static WorkflowParameters MakeParams(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
        return new WorkflowParameters(dict);
    }

    private static WorkflowRequest MakeRequest(object parameters) =>
        new WorkflowRequest
        {
            WorkflowId = "evolution",
            NuzlockeId = "nuzlocke-1",
            Language = "en-US",
            Parameters = MakeParams(parameters)
        };

    [Fact]
    public async Task Evolution_TeamPokemon_UpdatesSpeciesTypesAndStats()
    {
        _state.Team.Add(new TeamMember
        {
            Nickname = "Sparky",
            Species = "pikachu",
            Level = 35,
            Types = new List<string> { "electric" },
            DVs = new[] { 8, 8, 8, 8, 8 },
            StatExp = new[] { 0, 0, 0, 0, 0 },
            Stats = new PokemonStats { HP = 90, Attack = 68, Defense = 55, Speed = 100, Special = 65 }
        });

        var result = await CreateWorkflow().ExecuteAsync(MakeRequest(new
        {
            nuzlocke_id = "nuzlocke-1",
            nickname = "Sparky",
            evolved_species = "Raichu",
            evolution_trigger = "stone"
        }));

        Assert.True(result.Success);
        Assert.Equal("raichu", _state.Team[0].Species);
        Assert.Contains("electric", _state.Team[0].Types);
        Assert.Equal(RaichuStats.HP, _state.Team[0].Stats.HP);
        Assert.Equal(RaichuStats.HP, _state.Team[0].MaxHP);
        Assert.Single(result.Mutations);
        Assert.Equal("evolution", result.Mutations[0].Type);
        Assert.Contains("pikachu", result.Mutations[0].Description);
        Assert.Contains("raichu", result.Mutations[0].Description);
    }

    [Fact]
    public async Task Evolution_PCPokemon_UpdatesCorrectly()
    {
        _state.PCStorage.Add(new StoredPokemon
        {
            Nickname = "Sparky",
            Species = "pikachu",
            Level = 35,
            DVs = new[] { 8, 8, 8, 8, 8 },
            StatExp = new[] { 0, 0, 0, 0, 0 },
            Stats = new PokemonStats { HP = 90 }
        });

        var result = await CreateWorkflow().ExecuteAsync(MakeRequest(new
        {
            nuzlocke_id = "nuzlocke-1",
            nickname = "Sparky",
            evolved_species = "Raichu"
        }));

        Assert.True(result.Success);
        Assert.Equal("raichu", _state.PCStorage[0].Species);
        Assert.Equal("pc", result.Data["location"]);
    }

    [Fact]
    public async Task Evolution_PokemonNotFound_ReturnsFail()
    {
        // Empty state — no Sparky
        var result = await CreateWorkflow().ExecuteAsync(MakeRequest(new
        {
            nuzlocke_id = "nuzlocke-1",
            nickname = "Ghost",
            evolved_species = "Raichu"
        }));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Ghost"));
    }

    [Fact]
    public async Task Evolution_UnknownSpecies_ReturnsFail()
    {
        _state.Team.Add(new TeamMember { Nickname = "Sparky", Species = "pikachu", Level = 35 });

        _mockPokeApi.Setup(p => p.GetPokemonSubsetAsync("fakemon", It.IsAny<int>()))
            .ReturnsAsync((PokemonSubset?)null);

        var result = await CreateWorkflow().ExecuteAsync(MakeRequest(new
        {
            nuzlocke_id = "nuzlocke-1",
            nickname = "Sparky",
            evolved_species = "fakemon"
        }));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("fakemon"));
    }

    [Fact]
    public async Task Evolution_StatsRecalculatedWithExistingDvsAndStatExp()
    {
        var customDvs = new[] { 15, 12, 10, 14, 11 };
        var customStatExp = new[] { 1000, 500, 0, 2000, 300 };

        _state.Team.Add(new TeamMember
        {
            Nickname = "Sparky",
            Species = "pikachu",
            Level = 40,
            DVs = customDvs,
            StatExp = customStatExp,
            Stats = new PokemonStats { HP = 95 }
        });

        await CreateWorkflow().ExecuteAsync(MakeRequest(new
        {
            nuzlocke_id = "nuzlocke-1",
            nickname = "Sparky",
            evolved_species = "Raichu"
        }));

        // Verify calculator was called with the pokemon's own DVs and StatExp
        _mockCalc.Verify(c => c.Calculate(
            It.IsAny<int[]>(),
            It.Is<int[]>(d => d[0] == 15 && d[1] == 12),
            It.Is<int[]>(s => s[0] == 1000 && s[3] == 2000),
            40, It.IsAny<float>()), Times.Once);
    }

    [Fact]
    public async Task Evolution_GeneratesLlmAdvice()
    {
        _state.Team.Add(new TeamMember
        {
            Nickname = "Sparky",
            Species = "pikachu",
            Level = 35,
            DVs = new[] { 8, 8, 8, 8, 8 },
            StatExp = new[] { 0, 0, 0, 0, 0 },
            Stats = new PokemonStats { HP = 90, Attack = 68, Defense = 55, Speed = 100, Special = 65 }
        });

        var result = await CreateWorkflow().ExecuteAsync(MakeRequest(new
        {
            nuzlocke_id = "nuzlocke-1",
            nickname = "Sparky",
            evolved_species = "Raichu",
            evolution_trigger = "stone"
        }));

        Assert.True(result.Success);
        Assert.NotNull(result.Advice);
        Assert.NotEmpty(result.Advice!);
        _mockAi.Verify(a => a.GetCompletionAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
