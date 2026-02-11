using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using System.Text.Json;

namespace es.vargontoc.nuzlocke.ai.Services;

/// <summary>
/// Service for executing tool calls requested by the AI
/// </summary>
public class ToolExecutor
{
    private readonly IStateManager _stateManager;
    private readonly IPokeApiConnector _pokeApiConnector;
    private readonly ILogger<ToolExecutor> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ToolExecutor(
        IStateManager stateManager,
        IPokeApiConnector pokeApiConnector,
        ILogger<ToolExecutor> logger)
    {
        _stateManager = stateManager;
        _pokeApiConnector = pokeApiConnector;
        _logger = logger;
    }

    /// <summary>
    /// Execute a tool call and return the result
    /// </summary>
    public virtual async Task<ToolCallResult> ExecuteAsync(ToolCall toolCall)
    {
        try
        {
            _logger.LogInformation("Executing tool call: Id={ToolCallId}, Name={ToolName}, Arguments={Args}",
                toolCall.Id, toolCall.Name, toolCall.ArgumentsJson);

            var result = toolCall.Name switch
            {
                "get_game_state" => await ExecuteGetGameStateAsync(toolCall),
                "add_to_team" => await ExecuteAddToTeamAsync(toolCall),
                "mark_as_dead" => await ExecuteMarkAsDeadAsync(toolCall),
                "move_to_pc" => await ExecuteMoveToPcAsync(toolCall),
                "record_encounter" => await ExecuteRecordEncounterAsync(toolCall),
                "get_pokemon" => await ExecuteGetPokemonAsync(toolCall),
                "get_move" => await ExecuteGetMoveAsync(toolCall),
                "get_type" => await ExecuteGetTypeAsync(toolCall),
                "get_ability" => await ExecuteGetAbilityAsync(toolCall),
                "get_item" => await ExecuteGetItemAsync(toolCall),
                _ => throw new InvalidOperationException($"Unknown tool: {toolCall.Name}")
            };

            _logger.LogInformation("Tool executed successfully: Id={ToolCallId}, Name={ToolName}", toolCall.Id, toolCall.Name);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool: Id={ToolCallId}, Name={ToolName}", toolCall.Id, toolCall.Name);

            return new ToolCallResult
            {
                ToolCallId = toolCall.Id,
                ToolName = toolCall.Name,
                Content = JsonSerializer.Serialize(new
                {
                    error = ex.Message,
                    success = false
                }, _jsonOptions)
            };
        }
    }

    private async Task<ToolCallResult> ExecuteGetGameStateAsync(ToolCall toolCall)
    {
        var state = await _stateManager.GetStateAsync();

        return new ToolCallResult
        {
            ToolCallId = toolCall.Id,
            ToolName = toolCall.Name,
            Content = JsonSerializer.Serialize(state, _jsonOptions)
        };
    }

    private async Task<ToolCallResult> ExecuteAddToTeamAsync(ToolCall toolCall)
    {
        var args = JsonSerializer.Deserialize<AddToTeamArgs>(toolCall.ArgumentsJson, _jsonOptions);

        if (args == null)
        {
            throw new InvalidOperationException("Invalid arguments for add_to_team");
        }

        var member = new TeamMember
        {
            Nickname = args.Nickname,
            Species = args.Species,
            Level = args.Level,
            CaughtAt = args.CaughtAt,
            CurrentHP = args.CurrentHP ?? 0,
            MaxHP = args.MaxHP ?? 0,
            Moves = string.IsNullOrEmpty(args.Moves)
                ? new List<string>()
                : args.Moves.Split(',').Select(m => m.Trim()).ToList()
        };

        var success = await _stateManager.AddToTeamAsync(member);

        return new ToolCallResult
        {
            ToolCallId = toolCall.Id,
            ToolName = toolCall.Name,
            Content = JsonSerializer.Serialize(new
            {
                success,
                message = success
                    ? $"Added {args.Nickname} ({args.Species}) to the team"
                    : "Failed to add Pokemon to team (team might be full)"
            }, _jsonOptions)
        };
    }

    private async Task<ToolCallResult> ExecuteMarkAsDeadAsync(ToolCall toolCall)
    {
        var args = JsonSerializer.Deserialize<MarkAsDeadArgs>(toolCall.ArgumentsJson, _jsonOptions);

        if (args == null)
        {
            throw new InvalidOperationException("Invalid arguments for mark_as_dead");
        }

        var success = await _stateManager.MarkAsDeadAsync(
            args.Nickname,
            args.DeathLocation,
            args.CauseOfDeath);

        return new ToolCallResult
        {
            ToolCallId = toolCall.Id,
            ToolName = toolCall.Name,
            Content = JsonSerializer.Serialize(new
            {
                success,
                message = success
                    ? $"Marked {args.Nickname} as dead at {args.DeathLocation}"
                    : "Failed to mark Pokemon as dead (Pokemon not found in team)"
            }, _jsonOptions)
        };
    }

    private async Task<ToolCallResult> ExecuteMoveToPcAsync(ToolCall toolCall)
    {
        var args = JsonSerializer.Deserialize<MoveToPcArgs>(toolCall.ArgumentsJson, _jsonOptions);

        if (args == null)
        {
            throw new InvalidOperationException("Invalid arguments for move_to_pc");
        }

        await _stateManager.MoveToPCAsync(args.Nickname);

        return new ToolCallResult
        {
            ToolCallId = toolCall.Id,
            ToolName = toolCall.Name,
            Content = JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Moved {args.Nickname} to PC"
            }, _jsonOptions)
        };
    }

    private async Task<ToolCallResult> ExecuteRecordEncounterAsync(ToolCall toolCall)
    {
        var args = JsonSerializer.Deserialize<RecordEncounterArgs>(toolCall.ArgumentsJson, _jsonOptions);

        if (args == null)
        {
            throw new InvalidOperationException("Invalid arguments for record_encounter");
        }

        var success = await _stateManager.RecordEncounterAsync(
            args.Location,
            args.CapturedSpecies,
            args.CapturedNickname);

        return new ToolCallResult
        {
            ToolCallId = toolCall.Id,
            ToolName = toolCall.Name,
            Content = JsonSerializer.Serialize(new
            {
                success,
                message = success
                    ? $"Recorded encounter at {args.Location}" +
                      (args.CapturedSpecies != null ? $" - Caught {args.CapturedSpecies}" : "")
                    : "Failed to record encounter (location might already have an encounter)"
            }, _jsonOptions)
        };
    }

    // Argument classes for deserialization
    private class AddToTeamArgs
    {
        public string Nickname { get; set; } = string.Empty;
        public string Species { get; set; } = string.Empty;
        public int Level { get; set; }
        public string CaughtAt { get; set; } = string.Empty;
        public int? CurrentHP { get; set; }
        public int? MaxHP { get; set; }
        public string? Moves { get; set; }
    }

    private class MarkAsDeadArgs
    {
        public string Nickname { get; set; } = string.Empty;
        public string DeathLocation { get; set; } = string.Empty;
        public string CauseOfDeath { get; set; } = string.Empty;
    }

    private class MoveToPcArgs
    {
        public string Nickname { get; set; } = string.Empty;
    }

    private class RecordEncounterArgs
    {
        public string Location { get; set; } = string.Empty;
        public string? CapturedSpecies { get; set; }
        public string? CapturedNickname { get; set; }
    }

    private async Task<ToolCallResult> ExecuteGetPokemonAsync(ToolCall toolCall)
    {
        var args = JsonSerializer.Deserialize<PokeApiArgs>(toolCall.ArgumentsJson, _jsonOptions);

        if (args == null || string.IsNullOrEmpty(args.NameOrId))
        {
            throw new InvalidOperationException("Invalid arguments for get_pokemon");
        }

        _logger.LogDebug("Tool get_pokemon: calling connector for {NameOrId}", args.NameOrId);
        var sw1 = System.Diagnostics.Stopwatch.StartNew();
        var pokemon = await _pokeApiConnector.GetPokemonAsync(args.NameOrId);
        sw1.Stop();
        _logger.LogDebug("Tool get_pokemon: connector returned for {NameOrId} in {Ms}ms", args.NameOrId, sw1.Elapsed.TotalMilliseconds);

        return new ToolCallResult
        {
            ToolCallId = toolCall.Id,
            ToolName = toolCall.Name,
            Content = pokemon != null
                ? JsonSerializer.Serialize(pokemon, _jsonOptions)
                : JsonSerializer.Serialize(new { error = $"Pokemon '{args.NameOrId}' not found" }, _jsonOptions)
        };
    }

    private async Task<ToolCallResult> ExecuteGetMoveAsync(ToolCall toolCall)
    {
        var args = JsonSerializer.Deserialize<PokeApiArgs>(toolCall.ArgumentsJson, _jsonOptions);

        if (args == null || string.IsNullOrEmpty(args.NameOrId))
        {
            throw new InvalidOperationException("Invalid arguments for get_move");
        }

        _logger.LogDebug("Tool get_move: calling connector for {NameOrId}", args.NameOrId);
        var sw2 = System.Diagnostics.Stopwatch.StartNew();
        var move = await _pokeApiConnector.GetMoveAsync(args.NameOrId);
        sw2.Stop();
        _logger.LogDebug("Tool get_move: connector returned for {NameOrId} in {Ms}ms", args.NameOrId, sw2.Elapsed.TotalMilliseconds);

        return new ToolCallResult
        {
            ToolCallId = toolCall.Id,
            ToolName = toolCall.Name,
            Content = move != null
                ? JsonSerializer.Serialize(move, _jsonOptions)
                : JsonSerializer.Serialize(new { error = $"Move '{args.NameOrId}' not found" }, _jsonOptions)
        };
    }

    private async Task<ToolCallResult> ExecuteGetTypeAsync(ToolCall toolCall)
    {
        var args = JsonSerializer.Deserialize<PokeApiArgs>(toolCall.ArgumentsJson, _jsonOptions);

        if (args == null || string.IsNullOrEmpty(args.NameOrId))
        {
            throw new InvalidOperationException("Invalid arguments for get_type");
        }

        _logger.LogDebug("Tool get_type: calling connector for {NameOrId}", args.NameOrId);
        var sw3 = System.Diagnostics.Stopwatch.StartNew();
        var type = await _pokeApiConnector.GetTypeAsync(args.NameOrId);
        sw3.Stop();
        _logger.LogDebug("Tool get_type: connector returned for {NameOrId} in {Ms}ms", args.NameOrId, sw3.Elapsed.TotalMilliseconds);

        return new ToolCallResult
        {
            ToolCallId = toolCall.Id,
            ToolName = toolCall.Name,
            Content = type != null
                ? JsonSerializer.Serialize(type, _jsonOptions)
                : JsonSerializer.Serialize(new { error = $"Type '{args.NameOrId}' not found" }, _jsonOptions)
        };
    }

    private async Task<ToolCallResult> ExecuteGetAbilityAsync(ToolCall toolCall)
    {
        var args = JsonSerializer.Deserialize<PokeApiArgs>(toolCall.ArgumentsJson, _jsonOptions);

        if (args == null || string.IsNullOrEmpty(args.NameOrId))
        {
            throw new InvalidOperationException("Invalid arguments for get_ability");
        }

        _logger.LogDebug("Tool get_ability: calling connector for {NameOrId}", args.NameOrId);
        var sw4 = System.Diagnostics.Stopwatch.StartNew();
        var ability = await _pokeApiConnector.GetAbilityAsync(args.NameOrId);
        sw4.Stop();
        _logger.LogDebug("Tool get_ability: connector returned for {NameOrId} in {Ms}ms", args.NameOrId, sw4.Elapsed.TotalMilliseconds);

        return new ToolCallResult
        {
            ToolCallId = toolCall.Id,
            ToolName = toolCall.Name,
            Content = ability != null
                ? JsonSerializer.Serialize(ability, _jsonOptions)
                : JsonSerializer.Serialize(new { error = $"Ability '{args.NameOrId}' not found" }, _jsonOptions)
        };
    }

    private async Task<ToolCallResult> ExecuteGetItemAsync(ToolCall toolCall)
    {
        var args = JsonSerializer.Deserialize<PokeApiArgs>(toolCall.ArgumentsJson, _jsonOptions);

        if (args == null || string.IsNullOrEmpty(args.NameOrId))
        {
            throw new InvalidOperationException("Invalid arguments for get_item");
        }

        _logger.LogDebug("Tool get_item: calling connector for {NameOrId}", args.NameOrId);
        var sw5 = System.Diagnostics.Stopwatch.StartNew();
        var item = await _pokeApiConnector.GetItemAsync(args.NameOrId);
        sw5.Stop();
        _logger.LogDebug("Tool get_item: connector returned for {NameOrId} in {Ms}ms", args.NameOrId, sw5.Elapsed.TotalMilliseconds);

        return new ToolCallResult
        {
            ToolCallId = toolCall.Id,
            ToolName = toolCall.Name,
            Content = item != null
                ? JsonSerializer.Serialize(item, _jsonOptions)
                : JsonSerializer.Serialize(new { error = $"Item '{args.NameOrId}' not found" }, _jsonOptions)
        };
    }

    private class PokeApiArgs
    {
        public string NameOrId { get; set; } = string.Empty;
    }
}
