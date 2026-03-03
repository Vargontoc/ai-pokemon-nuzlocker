using es.vargontoc.nuzlocke.ai.Models;

namespace es.vargontoc.nuzlocke.ai.Services;

public class StateManager : IStateManager
{
    private readonly ILogger<StateManager> _logger;
    private readonly INuzlockeRepository _repository;

    public StateManager(ILogger<StateManager> logger, INuzlockeRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public Task<NuzlockeState> GetStateAsync() => GetStateAsync("default");

    public async Task<NuzlockeState> GetStateAsync(string sessionId)
    {
        try
        {
            return await _repository.GetGameStateAsync(sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading game state for {SessionId}", sessionId);
            return new NuzlockeState();
        }
    }

    public Task SaveStateAsync(NuzlockeState state) => SaveStateAsync("default", state);

    public async Task SaveStateAsync(string sessionId, NuzlockeState state)
    {
        try
        {
            await _repository.SaveGameStateAsync(sessionId, state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving game state for {SessionId}", sessionId);
        }
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

        state.Team.Remove(teamMember);

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

        state.Team.Remove(teamMember);

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

        _logger.LogInformation("Moved {Pokemon} ({Species}) to PC storage", nickname, teamMember.Species);
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

    public Task<bool> AddInventoryItemAsync(string itemName, int quantity, string category)
        => AddInventoryItemAsync("default", itemName, quantity, category);

    public async Task<bool> AddInventoryItemAsync(string sessionId, string itemName, int quantity, string category)
    {
        var state = await GetStateAsync(sessionId);

        var existing = state.Inventory.FirstOrDefault(i =>
            i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.Quantity += quantity;
            _logger.LogInformation("Updated inventory item {Item}: quantity now {Qty}", itemName, existing.Quantity);
        }
        else
        {
            state.Inventory.Add(new InventoryItem
            {
                Name = itemName,
                Quantity = quantity,
                Category = category
            });
            _logger.LogInformation("Added inventory item {Item} x{Qty} ({Category})", itemName, quantity, category);
        }

        await SaveStateAsync(sessionId, state);
        return true;
    }

    public Task<bool> UpdateMovesAsync(string nickname, List<string> moves)
        => UpdateMovesAsync("default", nickname, moves);

    public async Task<bool> UpdateMovesAsync(string sessionId, string nickname, List<string> moves)
    {
        var state = await GetStateAsync(sessionId);

        var pokemon = state.Team.FirstOrDefault(p =>
            p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));

        if (pokemon == null)
        {
            _logger.LogWarning("Cannot update moves for {Nickname}: not found in team", nickname);
            return false;
        }

        pokemon.Moves = moves;
        await SaveStateAsync(sessionId, state);
        _logger.LogInformation("Updated moves for {Nickname}: {Moves}", nickname, string.Join(", ", moves));
        return true;
    }

    // ── Battle context ────────────────────────────────────────────────────

    public Task<BattleContext> GetBattleContextAsync() => GetBattleContextAsync("default");

    public async Task<BattleContext> GetBattleContextAsync(string sessionId)
    {
        try
        {
            return await _repository.GetBattleStateAsync(sessionId) ?? new BattleContext();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading battle state for {SessionId}", sessionId);
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
        try
        {
            if (battleContext.InBattle)
                await _repository.SaveBattleStateAsync(sessionId, battleContext);
            else
                await _repository.DeleteBattleStateAsync(sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving battle state for {SessionId}", sessionId);
        }
    }
}
