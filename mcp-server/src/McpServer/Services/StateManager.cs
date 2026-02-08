using System.Text.Json;
using es.vargontoc.nuzlocke.ai.Models;

namespace es.vargontoc.nuzlocke.ai.Services;

public class StateManager : IStateManager
{
    private readonly string _stateFilePath;
    private readonly ILogger<StateManager> _logger;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public StateManager(ILogger<StateManager> logger, IConfiguration configuration)
    {
        _logger = logger;
        _stateFilePath = configuration.GetValue<string>("StateFilePath") ?? "session_state.json";
    }

    public async Task<NuzlockeState> GetStateAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            if (!File.Exists(_stateFilePath))
            {
                _logger.LogInformation("State file not found, creating new state");
                var newState = new NuzlockeState();
                await SaveStateInternalAsync(newState);
                return newState;
            }

            var json = await File.ReadAllTextAsync(_stateFilePath);
            var state = JsonSerializer.Deserialize<NuzlockeState>(json, _jsonOptions);

            if (state == null)
            {
                _logger.LogWarning("Failed to deserialize state, creating new state");
                return new NuzlockeState();
            }

            return state;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading state file");
            return new NuzlockeState();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SaveStateAsync(NuzlockeState state)
    {
        await _fileLock.WaitAsync();
        try
        {
            await SaveStateInternalAsync(state);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task SaveStateInternalAsync(NuzlockeState state)
    {
        state.LastUpdated = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(state, _jsonOptions);
        await File.WriteAllTextAsync(_stateFilePath, json);
        _logger.LogInformation("State saved to {FilePath}", _stateFilePath);
    }

    public async Task<bool> AddToTeamAsync(TeamMember pokemon)
    {
        var state = await GetStateAsync();

        if (state.Team.Count >= 6)
        {
            _logger.LogWarning("Cannot add {Pokemon} to team: team is full (6/6)", pokemon.Nickname);
            return false;
        }

        if (state.Team.Any(p => p.Nickname.Equals(pokemon.Nickname, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Cannot add {Pokemon} to team: nickname already exists", pokemon.Nickname);
            return false;
        }

        state.Team.Add(pokemon);
        await SaveStateAsync(state);

        _logger.LogInformation("Added {Pokemon} ({Species}) to team", pokemon.Nickname, pokemon.Species);
        return true;
    }

    public async Task<bool> MarkAsDeadAsync(string nickname, string deathLocation, string causeOfDeath)
    {
        var state = await GetStateAsync();

        var teamMember = state.Team.FirstOrDefault(p =>
            p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));

        if (teamMember == null)
        {
            _logger.LogWarning("Cannot mark {Nickname} as dead: not found in team", nickname);
            return false;
        }

        // Remove from team
        state.Team.Remove(teamMember);

        // Add to dead list
        var deadPokemon = new DeadPokemon
        {
            Nickname = teamMember.Nickname,
            Species = teamMember.Species,
            Level = teamMember.Level,
            DeathLocation = deathLocation,
            CauseOfDeath = causeOfDeath,
            DeathDate = DateTime.UtcNow,
            CaughtAt = teamMember.CaughtAt
        };

        state.DeadPokemon.Add(deadPokemon);
        await SaveStateAsync(state);

        _logger.LogInformation("Marked {Pokemon} ({Species}) as dead at {Location}",
            nickname, teamMember.Species, deathLocation);
        return true;
    }

    public async Task<bool> MoveToPCAsync(string nickname)
    {
        var state = await GetStateAsync();

        var teamMember = state.Team.FirstOrDefault(p =>
            p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));

        if (teamMember == null)
        {
            _logger.LogWarning("Cannot move {Nickname} to PC: not found in team", nickname);
            return false;
        }

        // Remove from team
        state.Team.Remove(teamMember);

        // Add to PC storage
        var storedPokemon = new StoredPokemon
        {
            Nickname = teamMember.Nickname,
            Species = teamMember.Species,
            Level = teamMember.Level,
            Moves = teamMember.Moves,
            CaughtAt = teamMember.CaughtAt,
            CaughtDate = teamMember.CaughtDate
        };

        state.PCStorage.Add(storedPokemon);
        await SaveStateAsync(state);

        _logger.LogInformation("Moved {Pokemon} ({Species}) to PC storage",
            nickname, teamMember.Species);
        return true;
    }

    public async Task<bool> RecordEncounterAsync(string location, string? capturedSpecies = null, string? capturedNickname = null)
    {
        var state = await GetStateAsync();

        if (state.Encounters.ContainsKey(location))
        {
            _logger.LogWarning("Encounter already recorded for {Location}", location);
            return false;
        }

        var encounter = new EncounterRecord
        {
            Location = location,
            CapturedSpecies = capturedSpecies,
            CapturedNickname = capturedNickname,
            EncounterUsed = capturedSpecies != null,
            EncounterDate = DateTime.UtcNow
        };

        state.Encounters[location] = encounter;
        await SaveStateAsync(state);

        _logger.LogInformation("Recorded encounter at {Location}: {Species}",
            location, capturedSpecies ?? "none");
        return true;
    }
}
