using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Workflows;
using System.Text.Json;

namespace es.vargontoc.nuzlocke.ai.Services;

/// <summary>
/// Service for executing tool calls requested by the AI
/// </summary>
public class ToolExecutor
{
    private readonly IStateManager _stateManager;
    private readonly IPokeApiConnector _pokeApiConnector;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly ILogger<ToolExecutor> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ToolExecutor(
        IStateManager stateManager,
        IPokeApiConnector pokeApiConnector,
        IWorkflowEngine workflowEngine,
        ILogger<ToolExecutor> logger)
    {
        _stateManager = stateManager;
        _pokeApiConnector = pokeApiConnector;
        _workflowEngine = workflowEngine;
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
                "start_battle" => await ExecuteStartBattleAsync(toolCall),
                "add_battle_log" => await ExecuteAddBattleLogAsync(toolCall),
                "end_battle" => await ExecuteEndBattleAsync(toolCall),
                "get_pokemon" => await ExecuteGetPokemonAsync(toolCall),
                "get_move" => await ExecuteGetMoveAsync(toolCall),
                "get_type" => await ExecuteGetTypeAsync(toolCall),
                "get_ability" => await ExecuteGetAbilityAsync(toolCall),
                "get_item" => await ExecuteGetItemAsync(toolCall),
                "execute_workflow" => await ExecuteWorkflowAsync(toolCall),
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
        // Try to parse optional sessionId from arguments
        string? sessionId = null;
        try
        {
            var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(toolCall.ArgumentsJson) ? "{}" : toolCall.ArgumentsJson);
            if (doc.RootElement.TryGetProperty("sessionId", out var sidEl) && sidEl.ValueKind == JsonValueKind.String)
            {
                sessionId = sidEl.GetString();
            }
        }
        catch { }

        var state = string.IsNullOrEmpty(sessionId)
            ? await _stateManager.GetStateAsync()
            : await _stateManager.GetStateAsync(sessionId);

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

        var success = string.IsNullOrEmpty(args.SessionId)
            ? await _stateManager.AddToTeamAsync(member)
            : await _stateManager.AddToTeamAsync(args.SessionId, member);

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

        var success = string.IsNullOrEmpty(args.SessionId)
            ? await _stateManager.MarkAsDeadAsync(
                args.Nickname,
                args.DeathLocation,
                args.CauseOfDeath)
            : await _stateManager.MarkAsDeadAsync(
                args.SessionId,
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

        if (string.IsNullOrEmpty(args.SessionId))
        {
            await _stateManager.MoveToPCAsync(args.Nickname);
        }
        else
        {
            await _stateManager.MoveToPCAsync(args.SessionId, args.Nickname);
        }

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

        var success = string.IsNullOrEmpty(args.SessionId)
            ? await _stateManager.RecordEncounterAsync(
                args.Location,
                args.CapturedSpecies,
                args.CapturedNickname)
            : await _stateManager.RecordEncounterAsync(
                args.SessionId,
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
        public string? SessionId { get; set; }
    }

    private class MarkAsDeadArgs
    {
        public string Nickname { get; set; } = string.Empty;
        public string DeathLocation { get; set; } = string.Empty;
        public string CauseOfDeath { get; set; } = string.Empty;
        public string? SessionId { get; set; }
    }

    private class MoveToPcArgs
    {
        public string Nickname { get; set; } = string.Empty;
        public string? SessionId { get; set; }
    }

    private class RecordEncounterArgs
    {
        public string Location { get; set; } = string.Empty;
        public string? CapturedSpecies { get; set; }
        public string? CapturedNickname { get; set; }
        public string? SessionId { get; set; }
    }

    private async Task<ToolCallResult> ExecuteStartBattleAsync(ToolCall toolCall)
    {
        var args = JsonSerializer.Deserialize<StartBattleArgs>(toolCall.ArgumentsJson, _jsonOptions);
        if (args == null || string.IsNullOrEmpty(args.OpponentName))
            throw new InvalidOperationException("Invalid arguments for start_battle");

        var battleContext = string.IsNullOrEmpty(args.SessionId)
            ? await _stateManager.StartBattleAsync(args.OpponentName, args.ActivePokemonNickname, args.BattleType)
            : await _stateManager.StartBattleAsync(args.SessionId, args.OpponentName, args.ActivePokemonNickname, args.BattleType);

        return new ToolCallResult
        {
            ToolCallId = toolCall.Id,
            ToolName = toolCall.Name,
            Content = JsonSerializer.Serialize(new
            {
                success = true,
                message = $"Battle started against {args.OpponentName}",
                battleContext
            }, _jsonOptions)
        };
    }

    private async Task<ToolCallResult> ExecuteAddBattleLogAsync(ToolCall toolCall)
    {
        var args = JsonSerializer.Deserialize<AddBattleLogArgs>(toolCall.ArgumentsJson, _jsonOptions);
        if (args == null || string.IsNullOrEmpty(args.LogEntry))
            throw new InvalidOperationException("Invalid arguments for add_battle_log");

        var success = string.IsNullOrEmpty(args.SessionId)
            ? await _stateManager.AddBattleLogAsync(args.LogEntry)
            : await _stateManager.AddBattleLogAsync(args.SessionId, args.LogEntry);

        return new ToolCallResult
        {
            ToolCallId = toolCall.Id,
            ToolName = toolCall.Name,
            Content = JsonSerializer.Serialize(new
            {
                success,
                message = success ? "Battle log entry added" : "No active battle to log to"
            }, _jsonOptions)
        };
    }

    private async Task<ToolCallResult> ExecuteEndBattleAsync(ToolCall toolCall)
    {
        string? sessionId = null;
        try
        {
            var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(toolCall.ArgumentsJson) ? "{}" : toolCall.ArgumentsJson);
            if (doc.RootElement.TryGetProperty("sessionId", out var sidEl) && sidEl.ValueKind == JsonValueKind.String)
                sessionId = sidEl.GetString();
        }
        catch { }

        var success = string.IsNullOrEmpty(sessionId)
            ? await _stateManager.EndBattleAsync()
            : await _stateManager.EndBattleAsync(sessionId);

        return new ToolCallResult
        {
            ToolCallId = toolCall.Id,
            ToolName = toolCall.Name,
            Content = JsonSerializer.Serialize(new
            {
                success,
                message = success ? "Battle ended" : "No active battle to end"
            }, _jsonOptions)
        };
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

    private async Task<ToolCallResult> ExecuteWorkflowAsync(ToolCall toolCall)
    {
        var args = JsonSerializer.Deserialize<ExecuteWorkflowArgs>(toolCall.ArgumentsJson, _jsonOptions);
        if (args == null || string.IsNullOrEmpty(args.WorkflowId))
            throw new InvalidOperationException("Invalid arguments for execute_workflow: workflowId is required");

        _logger.LogInformation("execute_workflow tool: {WorkflowId}, params={Params}",
            args.WorkflowId, args.Parameters);

        // Parse the parameters JSON string into WorkflowParameters
        WorkflowParameters workflowParams;
        try
        {
            var paramsJson = string.IsNullOrWhiteSpace(args.Parameters) ? "{}" : args.Parameters;
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(paramsJson, _jsonOptions);
            workflowParams = new WorkflowParameters(dict ?? new Dictionary<string, JsonElement>());
        }
        catch (JsonException ex)
        {
            return new ToolCallResult
            {
                ToolCallId = toolCall.Id,
                ToolName = toolCall.Name,
                Content = JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Invalid parameters JSON: {ex.Message}"
                }, _jsonOptions)
            };
        }

        // Build the WorkflowRequest — use sessionId if provided in the parameters
        var sessionId = workflowParams.GetString("nuzlocke_id") ?? args.SessionId ?? "default";

        var request = new WorkflowRequest
        {
            WorkflowId = args.WorkflowId,
            SessionId = sessionId,
            Parameters = workflowParams,
            Language = args.Language ?? "en-US"
        };

        var result = await _workflowEngine.ExecuteAsync(request);

        return new ToolCallResult
        {
            ToolCallId = toolCall.Id,
            ToolName = toolCall.Name,
            Content = JsonSerializer.Serialize(result, _jsonOptions)
        };
    }

    private class ExecuteWorkflowArgs
    {
        public string WorkflowId { get; set; } = string.Empty;
        public string? Parameters { get; set; }
        public string? SessionId { get; set; }
        public string? Language { get; set; }
    }

    private class StartBattleArgs
    {
        public string OpponentName { get; set; } = string.Empty;
        public string? ActivePokemonNickname { get; set; }
        public string? BattleType { get; set; }
        public string? SessionId { get; set; }
    }

    private class AddBattleLogArgs
    {
        public string LogEntry { get; set; } = string.Empty;
        public string? SessionId { get; set; }
    }

    private class PokeApiArgs
    {
        public string NameOrId { get; set; } = string.Empty;
        public string? SessionId { get; set; }
    }
}
