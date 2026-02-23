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

// ---------------------------------------------------------------------------
// Gen1StatsCalculator tests
// ---------------------------------------------------------------------------

public class Gen1StatsCalculatorTests
{
    private readonly IStatsCalculator _calc = new Gen1StatsCalculator();

    // Pikachu Gen 1: HP base=35, Atk=55, Def=40, Spe=90, Sp=50
    // At level 50, DV=8, StatExp=0:
    //   HP  = floor(((35+8)*2 + 0) * 50/100) + 50 + 10 = floor(86*0.5)+60 = 43+60 = 103
    //   Atk = floor(((55+8)*2 + 0) * 50/100) + 5 = floor(63)+5 = 63+5 = 68
    [Fact]
    public void Calculate_PikachuLv50_DefaultDvs_ReturnsCorrectHP()
    {
        var result = _calc.Calculate(
            new[] { 35, 55, 40, 90, 50 },
            new[] { 8, 8, 8, 8, 8 },
            new[] { 0, 0, 0, 0, 0 },
            level: 50);

        Assert.Equal(103, result.HP);
    }

    [Fact]
    public void Calculate_PikachuLv50_DefaultDvs_ReturnsCorrectAttack()
    {
        var result = _calc.Calculate(
            new[] { 35, 55, 40, 90, 50 },
            new[] { 8, 8, 8, 8, 8 },
            new[] { 0, 0, 0, 0, 0 },
            level: 50);

        // Atk = floor(((55+8)*2)*50/100) + 5 = floor(63) + 5 = 68
        Assert.Equal(68, result.Attack);
    }

    [Fact]
    public void Calculate_NatureMultiplier_AppliedToNonHPStats()
    {
        var result09 = _calc.Calculate(
            new[] { 50, 50, 50, 50, 50 },
            new[] { 8, 8, 8, 8, 8 },
            new[] { 0, 0, 0, 0, 0 },
            level: 50, nature: 0.9f);

        var result11 = _calc.Calculate(
            new[] { 50, 50, 50, 50, 50 },
            new[] { 8, 8, 8, 8, 8 },
            new[] { 0, 0, 0, 0, 0 },
            level: 50, nature: 1.1f);

        var result10 = _calc.Calculate(
            new[] { 50, 50, 50, 50, 50 },
            new[] { 8, 8, 8, 8, 8 },
            new[] { 0, 0, 0, 0, 0 },
            level: 50, nature: 1.0f);

        // x0.9 < neutral < x1.1 for non-HP stats
        Assert.True(result09.Attack < result10.Attack);
        Assert.True(result11.Attack > result10.Attack);
        // HP is not affected by nature
        Assert.Equal(result10.HP, result09.HP);
        Assert.Equal(result10.HP, result11.HP);
    }

    [Fact]
    public void Calculate_MaxDvAndMaxStatExp_NoOverflow()
    {
        // Should not throw, values should be positive
        var result = _calc.Calculate(
            new[] { 255, 255, 255, 255, 255 },
            new[] { 15, 15, 15, 15, 15 },
            new[] { 65535, 65535, 65535, 65535, 65535 },
            level: 100);

        Assert.True(result.HP > 0);
        Assert.True(result.Attack > 0);
        Assert.True(result.Defense > 0);
        Assert.True(result.Speed > 0);
        Assert.True(result.Special > 0);
    }
}

// ---------------------------------------------------------------------------
// LevelUpWorkflow tests
// ---------------------------------------------------------------------------

public class LevelUpWorkflowTests
{
    private readonly Mock<IStateManager> _mockState = new();
    private readonly Mock<IPokeApiConnector> _mockPokeApi = new();
    private readonly Mock<IAiProvider> _mockAi = new();
    private readonly Mock<INuzlockeFileManager> _mockFileManager = new();
    private readonly Mock<IStatsCalculator> _mockCalc = new();
    private readonly Mock<ILogger<LevelUpWorkflow>> _mockLogger = new();

    private NuzlockeState _state = new();

    private static readonly PokemonStats FakeStats = new() { HP = 60, Attack = 55, Defense = 40, Speed = 90, Special = 50 };

    public LevelUpWorkflowTests()
    {
        _mockFileManager.Setup(f => f.GetNuzlockePathAsync("nuzlocke-1"))
            .ReturnsAsync("/tmp/nuzlocke-1");

        _mockState.Setup(s => s.GetStateAsync("nuzlocke-1"))
            .ReturnsAsync(() => _state);
        _mockState.Setup(s => s.GetBattleContextAsync("nuzlocke-1"))
            .ReturnsAsync(new BattleContext());
        _mockState.Setup(s => s.SaveStateAsync("nuzlocke-1", It.IsAny<NuzlockeState>()))
            .Returns(Task.CompletedTask);

        _mockPokeApi.Setup(p => p.GetPokemonSubsetAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new PokemonSubset
            {
                Name = "pikachu",
                Stats = new Dictionary<string, int> { { "hp", 35 }, { "attack", 55 }, { "defense", 40 }, { "speed", 90 }, { "special-attack", 50 } }
            });

        _mockCalc.Setup(c => c.Calculate(
            It.IsAny<int[]>(), It.IsAny<int[]>(), It.IsAny<int[]>(), It.IsAny<int>(), It.IsAny<float>()))
            .Returns(FakeStats);
    }

    private LevelUpWorkflow CreateWorkflow() =>
        new(_mockState.Object, _mockPokeApi.Object, _mockAi.Object,
            _mockFileManager.Object, _mockCalc.Object, _mockLogger.Object);

    private static WorkflowParameters MakeParams(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
        return new WorkflowParameters(dict);
    }

    private static WorkflowRequest MakeRequest(object parameters) =>
        new WorkflowRequest
        {
            WorkflowId = "level_up",
            SessionId = "nuzlocke-1",
            Language = "en-US",
            Parameters = MakeParams(parameters)
        };

    [Fact]
    public async Task LevelUp_TeamPokemon_LevelsUpAndRecalcStats()
    {
        _state.Team.Add(new TeamMember { Nickname = "Sparky", Species = "pikachu", Level = 15,
            DVs = new[] { 8, 8, 8, 8, 8 }, StatExp = new[] { 0, 0, 0, 0, 0 } });

        var result = await CreateWorkflow().ExecuteAsync(MakeRequest(new
        {
            nuzlocke_id = "nuzlocke-1",
            nickname = "Sparky",
            new_level = 16
        }));

        Assert.True(result.Success);
        Assert.Equal(16, _state.Team[0].Level);
        Assert.Equal(FakeStats.HP, _state.Team[0].Stats.HP);
        Assert.Equal(FakeStats.HP, _state.Team[0].MaxHP);
        Assert.Single(result.Mutations);
        Assert.Equal("level_up", result.Mutations[0].Type);
    }

    [Fact]
    public async Task LevelUp_PCPokemon_LevelsUpCorrectly()
    {
        _state.PCStorage.Add(new StoredPokemon { Nickname = "Boxed", Species = "rattata", Level = 10,
            DVs = new[] { 8, 8, 8, 8, 8 }, StatExp = new[] { 0, 0, 0, 0, 0 } });

        var result = await CreateWorkflow().ExecuteAsync(MakeRequest(new
        {
            nuzlocke_id = "nuzlocke-1",
            nickname = "Boxed",
            new_level = 11
        }));

        Assert.True(result.Success);
        Assert.Equal(11, _state.PCStorage[0].Level);
        Assert.Equal("pc", result.Data["location"]);
    }

    [Fact]
    public async Task LevelUp_NoNewLevelParam_AutoIncrements()
    {
        _state.Team.Add(new TeamMember { Nickname = "Sparky", Species = "pikachu", Level = 20,
            DVs = new[] { 8, 8, 8, 8, 8 }, StatExp = new[] { 0, 0, 0, 0, 0 } });

        var result = await CreateWorkflow().ExecuteAsync(MakeRequest(new
        {
            nuzlocke_id = "nuzlocke-1",
            nickname = "Sparky"
            // no new_level → auto-increment
        }));

        Assert.True(result.Success);
        Assert.Equal(21, _state.Team[0].Level);
        Assert.Equal(21, result.Data["new_level"]);
    }

    [Fact]
    public async Task LevelUp_PokemonNotFound_ReturnsFail()
    {
        // State is empty — no Sparky
        var result = await CreateWorkflow().ExecuteAsync(MakeRequest(new
        {
            nuzlocke_id = "nuzlocke-1",
            nickname = "Ghost"
        }));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Ghost"));
    }

    [Fact]
    public async Task LevelUp_NewLevelLowerThanCurrent_ReturnsFail()
    {
        _state.Team.Add(new TeamMember { Nickname = "Sparky", Species = "pikachu", Level = 30,
            DVs = new[] { 8, 8, 8, 8, 8 }, StatExp = new[] { 0, 0, 0, 0, 0 } });

        var result = await CreateWorkflow().ExecuteAsync(MakeRequest(new
        {
            nuzlocke_id = "nuzlocke-1",
            nickname = "Sparky",
            new_level = 25
        }));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("greater than current level"));
    }
}
