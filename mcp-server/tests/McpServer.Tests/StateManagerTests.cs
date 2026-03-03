using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests;

public class StateManagerTests
{
    private readonly Mock<INuzlockeRepository> _mockRepository = new();
    private readonly StateManager _stateManager;
    private NuzlockeState _state = new();
    private BattleContext _battleContext = new();

    public StateManagerTests()
    {
        _mockRepository.Setup(r => r.GetGameStateAsync(It.IsAny<string>()))
            .ReturnsAsync(() => _state);
        _mockRepository.Setup(r => r.SaveGameStateAsync(It.IsAny<string>(), It.IsAny<NuzlockeState>()))
            .Callback<string, NuzlockeState>((_, s) => _state = s)
            .Returns(Task.CompletedTask);

        _mockRepository.Setup(r => r.GetBattleStateAsync(It.IsAny<string>()))
            .ReturnsAsync(() => _battleContext.InBattle ? _battleContext : null);
        _mockRepository.Setup(r => r.SaveBattleStateAsync(It.IsAny<string>(), It.IsAny<BattleContext>()))
            .Callback<string, BattleContext>((_, bc) => _battleContext = bc)
            .Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.DeleteBattleStateAsync(It.IsAny<string>()))
            .Callback<string>(_ => _battleContext = new BattleContext())
            .Returns(Task.CompletedTask);

        _stateManager = new StateManager(NullLogger<StateManager>.Instance, _mockRepository.Object);
    }

    [Fact]
    public async Task GetStateAsync_ReturnsCurrentState()
    {
        var state = await _stateManager.GetStateAsync();

        Assert.NotNull(state);
        Assert.Empty(state.Team);
        Assert.Empty(state.DeadPokemon);
        Assert.Empty(state.PCStorage);
        Assert.Empty(state.Encounters);
    }

    [Fact]
    public async Task AddToTeamAsync_AddsPokemonSuccessfully()
    {
        var pokemon = new TeamMember
        {
            Nickname = "Sparky",
            Species = "pikachu",
            Level = 10,
            CaughtAt = "Route 1"
        };

        var success = await _stateManager.AddToTeamAsync(pokemon);
        var state = await _stateManager.GetStateAsync();

        Assert.True(success);
        Assert.Single(state.Team);
        Assert.Equal("Sparky", state.Team[0].Nickname);
        Assert.Equal("pikachu", state.Team[0].Species);
    }

    [Fact]
    public async Task AddToTeamAsync_FailsWhenTeamIsFull()
    {
        for (int i = 1; i <= 6; i++)
        {
            await _stateManager.AddToTeamAsync(new TeamMember
            {
                Nickname = $"Pokemon{i}",
                Species = "bulbasaur",
                Level = 5,
                CaughtAt = "Pallet Town"
            });
        }

        var result = await _stateManager.AddToTeamAsync(new TeamMember
        {
            Nickname = "Seventh",
            Species = "charmander",
            Level = 5,
            CaughtAt = "Route 1"
        });

        var state = await _stateManager.GetStateAsync();

        Assert.False(result);
        Assert.Equal(6, state.Team.Count);
    }

    [Fact]
    public async Task MarkAsDeadAsync_MovesFromTeamToGraveyard()
    {
        await _stateManager.AddToTeamAsync(new TeamMember
        {
            Nickname = "Brave",
            Species = "pidgey",
            Level = 8,
            CaughtAt = "Route 1"
        });

        var success = await _stateManager.MarkAsDeadAsync("Brave", "Viridian Forest", "Defeated by wild Beedrill");
        var state = await _stateManager.GetStateAsync();

        Assert.True(success);
        Assert.Empty(state.Team);
        Assert.Single(state.DeadPokemon);
        Assert.Equal("Brave", state.DeadPokemon[0].Nickname);
        Assert.Equal("pidgey", state.DeadPokemon[0].Species);
        Assert.Equal("Viridian Forest", state.DeadPokemon[0].DeathLocation);
        Assert.Equal("Defeated by wild Beedrill", state.DeadPokemon[0].CauseOfDeath);
    }

    [Fact]
    public async Task MoveToPCAsync_MovesFromTeamToPC()
    {
        await _stateManager.AddToTeamAsync(new TeamMember
        {
            Nickname = "Boxed",
            Species = "rattata",
            Level = 3,
            CaughtAt = "Route 1",
            Moves = new List<string> { "tackle", "tail-whip" }
        });

        var success = await _stateManager.MoveToPCAsync("Boxed");
        var state = await _stateManager.GetStateAsync();

        Assert.True(success);
        Assert.Empty(state.Team);
        Assert.Single(state.PCStorage);
        Assert.Equal("Boxed", state.PCStorage[0].Nickname);
        Assert.Equal("rattata", state.PCStorage[0].Species);
        Assert.Equal(2, state.PCStorage[0].Moves.Count);
    }

    [Fact]
    public async Task RecordEncounterAsync_RecordsFirstEncounter()
    {
        var success = await _stateManager.RecordEncounterAsync("Route 1", "pidgey", "Birdy");
        var state = await _stateManager.GetStateAsync();

        Assert.True(success);
        Assert.Single(state.Encounters);
        Assert.True(state.Encounters.ContainsKey("Route 1"));
        Assert.Equal("pidgey", state.Encounters["Route 1"].CapturedSpecies);
        Assert.Equal("Birdy", state.Encounters["Route 1"].CapturedNickname);
        Assert.True(state.Encounters["Route 1"].EncounterUsed);
    }

    [Fact]
    public async Task RecordEncounterAsync_FailsForDuplicateLocation()
    {
        await _stateManager.RecordEncounterAsync("Route 1", "pidgey", "Birdy");

        var success = await _stateManager.RecordEncounterAsync("Route 1", "rattata", "Ratty");

        Assert.False(success);
    }

    // ========== BATTLE CONTEXT TESTS ==========

    [Fact]
    public async Task GetBattleContextAsync_ReturnsEmptyWhenNoBattle()
    {
        var bc = await _stateManager.GetBattleContextAsync();
        Assert.False(bc.InBattle);
    }

    [Fact]
    public async Task StartBattleAsync_CreatesNewBattleContext()
    {
        var bc = await _stateManager.StartBattleAsync("Gym Leader Brock", "Sparky", "gym_leader");

        Assert.True(bc.InBattle);
        Assert.Equal("Gym Leader Brock", bc.OpponentName);
        Assert.Equal("Sparky", bc.ActivePokemonNickname);
        Assert.Equal("gym_leader", bc.BattleType);
        Assert.Equal(0, bc.TurnCount);
        Assert.Empty(bc.BattleLog);
        Assert.NotNull(bc.BattleStartedAt);
    }

    [Fact]
    public async Task StartBattleAsync_OverwritesPreviousBattle()
    {
        await _stateManager.StartBattleAsync("Wild Rattata");
        await _stateManager.AddBattleLogAsync("Sparky used Tackle");

        var bc = await _stateManager.StartBattleAsync("Gym Leader Misty");

        Assert.True(bc.InBattle);
        Assert.Equal("Gym Leader Misty", bc.OpponentName);
        Assert.Equal(0, bc.TurnCount);
        Assert.Empty(bc.BattleLog);
    }

    [Fact]
    public async Task AddBattleLogAsync_AppendsEntryAndIncrementsTurn()
    {
        await _stateManager.StartBattleAsync("Wild Geodude");

        var ok1 = await _stateManager.AddBattleLogAsync("Sparky used Thunderbolt");
        var ok2 = await _stateManager.AddBattleLogAsync("Geodude fainted");

        var bc = await _stateManager.GetBattleContextAsync();

        Assert.True(ok1);
        Assert.True(ok2);
        Assert.Equal(2, bc.TurnCount);
        Assert.Equal(2, bc.BattleLog.Count);
        Assert.Contains("[Turn 1]", bc.BattleLog[0]);
        Assert.Contains("[Turn 2]", bc.BattleLog[1]);
    }

    [Fact]
    public async Task AddBattleLogAsync_ReturnsFalseWhenNoBattle()
    {
        var result = await _stateManager.AddBattleLogAsync("some event");
        Assert.False(result);
    }

    [Fact]
    public async Task EndBattleAsync_ClearsBattleContext()
    {
        await _stateManager.StartBattleAsync("Rival Blue");
        await _stateManager.AddBattleLogAsync("Sparky used Quick Attack");

        var ended = await _stateManager.EndBattleAsync();
        var bc = await _stateManager.GetBattleContextAsync();

        Assert.True(ended);
        Assert.False(bc.InBattle);
        Assert.Empty(bc.BattleLog);
    }

    [Fact]
    public async Task EndBattleAsync_ReturnsFalseWhenNoBattle()
    {
        var result = await _stateManager.EndBattleAsync();
        Assert.False(result);
    }
}
