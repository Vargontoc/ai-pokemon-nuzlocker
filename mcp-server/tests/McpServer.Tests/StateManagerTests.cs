using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests;

public class StateManagerTests : IDisposable
{
    private readonly StateManager _stateManager;
    private readonly string _testFilePath;

    public StateManagerTests()
    {
        _testFilePath = $"test_state_{Guid.NewGuid()}.json";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StateFilePath"] = _testFilePath
            })
            .Build();

        _stateManager = new StateManager(NullLogger<StateManager>.Instance, config);
    }

    [Fact]
    public async Task GetStateAsync_CreatesNewStateIfFileDoesNotExist()
    {
        // Act
        var state = await _stateManager.GetStateAsync();

        // Assert
        Assert.NotNull(state);
        Assert.Empty(state.Team);
        Assert.Empty(state.DeadPokemon);
        Assert.Empty(state.PCStorage);
        Assert.Empty(state.Encounters);
        Assert.True(File.Exists(_testFilePath));
    }

    [Fact]
    public async Task AddToTeamAsync_AddsPokemonSuccessfully()
    {
        // Arrange
        var pokemon = new TeamMember
        {
            Nickname = "Sparky",
            Species = "pikachu",
            Level = 10,
            CaughtAt = "Route 1"
        };

        // Act
        var success = await _stateManager.AddToTeamAsync(pokemon);
        var state = await _stateManager.GetStateAsync();

        // Assert
        Assert.True(success);
        Assert.Single(state.Team);
        Assert.Equal("Sparky", state.Team[0].Nickname);
        Assert.Equal("pikachu", state.Team[0].Species);
    }

    [Fact]
    public async Task AddToTeamAsync_FailsWhenTeamIsFull()
    {
        // Arrange - Add 6 Pokemon to fill the team
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

        // Act - Try to add a 7th Pokemon
        var result = await _stateManager.AddToTeamAsync(new TeamMember
        {
            Nickname = "Seventh",
            Species = "charmander",
            Level = 5,
            CaughtAt = "Route 1"
        });

        var state = await _stateManager.GetStateAsync();

        // Assert
        Assert.False(result);
        Assert.Equal(6, state.Team.Count);
    }

    [Fact]
    public async Task MarkAsDeadAsync_MovesFromTeamToGraveyard()
    {
        // Arrange
        await _stateManager.AddToTeamAsync(new TeamMember
        {
            Nickname = "Brave",
            Species = "pidgey",
            Level = 8,
            CaughtAt = "Route 1"
        });

        // Act
        var success = await _stateManager.MarkAsDeadAsync("Brave", "Viridian Forest", "Defeated by wild Beedrill");
        var state = await _stateManager.GetStateAsync();

        // Assert
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
        // Arrange
        await _stateManager.AddToTeamAsync(new TeamMember
        {
            Nickname = "Boxed",
            Species = "rattata",
            Level = 3,
            CaughtAt = "Route 1",
            Moves = new List<string> { "tackle", "tail-whip" }
        });

        // Act
        var success = await _stateManager.MoveToPCAsync("Boxed");
        var state = await _stateManager.GetStateAsync();

        // Assert
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
        // Act
        var success = await _stateManager.RecordEncounterAsync("Route 1", "pidgey", "Birdy");
        var state = await _stateManager.GetStateAsync();

        // Assert
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
        // Arrange
        await _stateManager.RecordEncounterAsync("Route 1", "pidgey", "Birdy");

        // Act - Try to record another encounter at same location
        var success = await _stateManager.RecordEncounterAsync("Route 1", "rattata", "Ratty");

        // Assert
        Assert.False(success); // Nuzlocke rule: only one Pokemon per route
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }
}
