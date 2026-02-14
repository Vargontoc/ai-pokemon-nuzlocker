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

    private readonly INuzlockeSessionManager? _sessionManager;

    public StateManager(ILogger<StateManager> logger, IConfiguration configuration, INuzlockeSessionManager? sessionManager = null)
    {
        _logger = logger;
        _sessionManager = sessionManager;
        _stateFilePath = configuration.GetValue<string>("StateFilePath") ?? "session_state.json";
    }

    public Task<NuzlockeState> GetStateAsync() => GetStateAsync("default");

    public async Task<NuzlockeState> GetStateAsync(string sessionId)
    {
        if (_sessionManager == null || sessionId == "default")
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

        // Use session manager to load per-session file
        try
        {
            var fileData = await _sessionManager.LoadSessionDataAsync(sessionId);
            return fileData.GameState ?? new NuzlockeState();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading session state for {SessionId}", sessionId);
            return new NuzlockeState();
        }
    }

    public Task SaveStateAsync(NuzlockeState state) => SaveStateAsync("default", state);

    public async Task SaveStateAsync(string sessionId, NuzlockeState state)
    {
        if (_sessionManager == null || sessionId == "default")
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
            return;
        }

        // Use session manager to persist
        try
        {
            var fileData = await _sessionManager.LoadSessionDataAsync(sessionId);
            fileData.GameState = state;
            await _sessionManager.SaveSessionDataAsync(sessionId, fileData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving session state for {SessionId}", sessionId);
        }
    }

    private async Task SaveStateInternalAsync(NuzlockeState state)
    {
        state.LastUpdated = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(state, _jsonOptions);
        await File.WriteAllTextAsync(_stateFilePath, json);
        _logger.LogInformation("State saved to {FilePath}", _stateFilePath);
    }

    public Task<bool> AddToTeamAsync(TeamMember pokemon) => AddToTeamAsync("default", pokemon);

    public async Task<bool> AddToTeamAsync(string sessionId, TeamMember pokemon)
    {
        var state = await GetStateAsync(sessionId);

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
        await SaveStateAsync(sessionId, state);

        _logger.LogInformation("Added {Pokemon} ({Species}) to team", pokemon.Nickname, pokemon.Species);
        return true;
    }

    public Task<bool> MarkAsDeadAsync(string nickname, string deathLocation, string causeOfDeath)
        => MarkAsDeadAsync("default", nickname, deathLocation, causeOfDeath);

    public async Task<bool> MarkAsDeadAsync(string sessionId, string nickname, string deathLocation, string causeOfDeath)
    {
        var state = await GetStateAsync(sessionId);

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
        await SaveStateAsync(sessionId, state);

        _logger.LogInformation("Marked {Pokemon} ({Species}) as dead at {Location}",
            nickname, teamMember.Species, deathLocation);
        return true;
    }

    public Task<bool> MoveToPCAsync(string nickname) => MoveToPCAsync("default", nickname);

    public async Task<bool> MoveToPCAsync(string sessionId, string nickname)
    {
        var state = await GetStateAsync(sessionId);

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
        await SaveStateAsync(sessionId, state);

        _logger.LogInformation("Moved {Pokemon} ({Species}) to PC storage",
            nickname, teamMember.Species);
        return true;
    }

    public Task<bool> RecordEncounterAsync(string location, string? capturedSpecies = null, string? capturedNickname = null)
        => RecordEncounterAsync("default", location, capturedSpecies, capturedNickname);

    public async Task<bool> RecordEncounterAsync(string sessionId, string location, string? capturedSpecies = null, string? capturedNickname = null)
    {
        var state = await GetStateAsync(sessionId);

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
        await SaveStateAsync(sessionId, state);

        _logger.LogInformation("Recorded encounter at {Location}: {Species}",
            location, capturedSpecies ?? "none");
        return true;
    }

    // ========== BATTLE CONTEXT METHODS ==========

    private string BattleContextFilePath => Path.ChangeExtension(_stateFilePath, ".battle.json");

    public Task<BattleContext> GetBattleContextAsync() => GetBattleContextAsync("default");

    public async Task<BattleContext> GetBattleContextAsync(string sessionId)
    {
        if (_sessionManager == null || sessionId == "default")
        {
            await _fileLock.WaitAsync();
            try
            {
                if (!File.Exists(BattleContextFilePath))
                    return new BattleContext();

                var json = await File.ReadAllTextAsync(BattleContextFilePath);
                return JsonSerializer.Deserialize<BattleContext>(json, _jsonOptions) ?? new BattleContext();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading battle context file");
                return new BattleContext();
            }
            finally { _fileLock.Release(); }
        }

        try
        {
            var fileData = await _sessionManager.LoadSessionDataAsync(sessionId);
            return fileData.BattleContext ?? new BattleContext();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading battle context for {SessionId}", sessionId);
            return new BattleContext();
        }
    }

    public Task<BattleContext> StartBattleAsync(string opponentName, string? activePokemonNickname = null, string? battleType = null)
        => StartBattleAsync("default", opponentName, activePokemonNickname, battleType);

    public async Task<BattleContext> StartBattleAsync(string sessionId, string opponentName, string? activePokemonNickname = null, string? battleType = null)
    {
        var battleContext = new BattleContext
        {
            InBattle = true,
            OpponentName = opponentName,
            ActivePokemonNickname = activePokemonNickname,
            TurnCount = 0,
            BattleLog = new List<string>(),
            BattleStartedAt = DateTime.UtcNow,
            BattleType = battleType
        };

        await SaveBattleContextAsync(sessionId, battleContext);
        _logger.LogInformation("Battle started against {Opponent} (session {SessionId})", opponentName, sessionId);
        return battleContext;
    }

    public Task<bool> AddBattleLogAsync(string logEntry) => AddBattleLogAsync("default", logEntry);

    public async Task<bool> AddBattleLogAsync(string sessionId, string logEntry)
    {
        var battleContext = await GetBattleContextAsync(sessionId);
        if (!battleContext.InBattle)
        {
            _logger.LogWarning("Cannot add battle log: no active battle (session {SessionId})", sessionId);
            return false;
        }

        battleContext.TurnCount++;
        battleContext.BattleLog.Add($"[Turn {battleContext.TurnCount}] {logEntry}");
        await SaveBattleContextAsync(sessionId, battleContext);
        _logger.LogInformation("Added battle log entry (turn {Turn}, session {SessionId})", battleContext.TurnCount, sessionId);
        return true;
    }

    public Task<bool> EndBattleAsync() => EndBattleAsync("default");

    public async Task<bool> EndBattleAsync(string sessionId)
    {
        var battleContext = await GetBattleContextAsync(sessionId);
        if (!battleContext.InBattle)
        {
            _logger.LogWarning("Cannot end battle: no active battle (session {SessionId})", sessionId);
            return false;
        }

        await SaveBattleContextAsync(sessionId, new BattleContext());
        _logger.LogInformation("Battle ended (session {SessionId})", sessionId);
        return true;
    }

    private async Task SaveBattleContextAsync(string sessionId, BattleContext battleContext)
    {
        if (_sessionManager == null || sessionId == "default")
        {
            await _fileLock.WaitAsync();
            try
            {
                var json = JsonSerializer.Serialize(battleContext, _jsonOptions);
                await File.WriteAllTextAsync(BattleContextFilePath, json);
            }
            finally { _fileLock.Release(); }
            return;
        }

        try
        {
            var fileData = await _sessionManager.LoadSessionDataAsync(sessionId);
            fileData.BattleContext = battleContext;
            await _sessionManager.SaveSessionDataAsync(sessionId, fileData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving battle context for {SessionId}", sessionId);
        }
    }
}
