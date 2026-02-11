using System.ComponentModel;
using System.Text.Json;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Services;
using ModelContextProtocol.Server;

namespace es.vargontoc.nuzlocke.ai.Tools;

[McpServerToolType]
public static class NuzlockeStateTools
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [McpServerTool]
    [Description("Get the complete current state of the Nuzlocke run, including team, dead Pokemon, PC storage, and encounters")]
    public static async Task<string> GetGameState(IStateManager stateManager, string? sessionId = null)
    {
        try
        {
            var state = string.IsNullOrEmpty(sessionId)
                ? await stateManager.GetStateAsync()
                : await stateManager.GetStateAsync(sessionId);
            return JsonSerializer.Serialize(state, _jsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Failed to get game state: {ex.Message}" }, _jsonOptions);
        }
    }

    [McpServerTool]
    [Description("Add a Pokemon to the current team (maximum 6 Pokemon allowed)")]
    public static async Task<string> AddToTeam(
        IStateManager stateManager,
        string? sessionId,
        [Description("Nickname for the Pokemon (required for Nuzlocke)")] string nickname,
        [Description("Species name (e.g., 'pikachu')")] string species,
        [Description("Current level")] int level,
        [Description("Location where caught")] string caughtAt,
        [Description("Current HP (optional)")] int? currentHP = null,
        [Description("Maximum HP (optional)")] int? maxHP = null,
        [Description("Comma-separated list of moves (optional)")] string? moves = null)
    {
        try
        {
            var pokemon = new TeamMember
            {
                Nickname = nickname,
                Species = species.ToLower(),
                Level = level,
                CaughtAt = caughtAt,
                CurrentHP = currentHP ?? 0,
                MaxHP = maxHP ?? 0,
                Moves = string.IsNullOrWhiteSpace(moves)
                    ? new List<string>()
                    : moves.Split(',').Select(m => m.Trim().ToLower()).ToList(),
                CaughtDate = DateTime.UtcNow
            };

            var success = string.IsNullOrEmpty(sessionId)
                ? await stateManager.AddToTeamAsync(pokemon)
                : await stateManager.AddToTeamAsync(sessionId, pokemon);

            if (success)
            {
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = $"{nickname} ({species}) added to team successfully",
                    teamSize = (await (string.IsNullOrEmpty(sessionId) ? stateManager.GetStateAsync() : stateManager.GetStateAsync(sessionId))).Team.Count
                }, _jsonOptions);
            }
            else
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Failed to add Pokemon to team. Team may be full (6/6) or nickname already exists"
                }, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { success = false, error = ex.Message }, _jsonOptions);
        }
    }

    [McpServerTool]
    [Description("Mark a Pokemon as dead and move it to the graveyard (Nuzlocke rule: fainted Pokemon are considered dead)")]
    public static async Task<string> MarkAsDead(
        IStateManager stateManager,
        string? sessionId,
        [Description("Nickname of the Pokemon that died")] string nickname,
        [Description("Location where the Pokemon died")] string deathLocation,
        [Description("Cause of death (e.g., 'Defeated by Gym Leader's Machamp')")] string causeOfDeath)
    {
        try
        {
            var success = string.IsNullOrEmpty(sessionId)
                ? await stateManager.MarkAsDeadAsync(nickname, deathLocation, causeOfDeath)
                : await stateManager.MarkAsDeadAsync(sessionId, nickname, deathLocation, causeOfDeath);

            if (success)
            {
                var state = string.IsNullOrEmpty(sessionId)
                    ? await stateManager.GetStateAsync()
                    : await stateManager.GetStateAsync(sessionId);
                var deadPokemon = state.DeadPokemon.Last();

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = $"{nickname} has been marked as dead",
                    deadPokemon = new
                    {
                        deadPokemon.Nickname,
                        deadPokemon.Species,
                        deadPokemon.Level,
                        deadPokemon.DeathLocation,
                        deadPokemon.CauseOfDeath
                    },
                    totalDeaths = state.DeadPokemon.Count
                }, _jsonOptions);
            }
            else
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Failed to mark {nickname} as dead. Pokemon not found in team"
                }, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { success = false, error = ex.Message }, _jsonOptions);
        }
    }

    [McpServerTool]
    [Description("Move a Pokemon from the team to PC storage")]
    public static async Task<string> MoveToPC(
        IStateManager stateManager,
        string? sessionId,
        [Description("Nickname of the Pokemon to move to PC")] string nickname)
    {
        try
        {
            var success = string.IsNullOrEmpty(sessionId)
                ? await stateManager.MoveToPCAsync(nickname)
                : await stateManager.MoveToPCAsync(sessionId, nickname);

            if (success)
            {
                var state = string.IsNullOrEmpty(sessionId)
                    ? await stateManager.GetStateAsync()
                    : await stateManager.GetStateAsync(sessionId);

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = $"{nickname} has been moved to PC storage",
                    teamSize = state.Team.Count,
                    pcSize = state.PCStorage.Count
                }, _jsonOptions);
            }
            else
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Failed to move {nickname} to PC. Pokemon not found in team"
                }, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { success = false, error = ex.Message }, _jsonOptions);
        }
    }

    [McpServerTool]
    [Description("Record an encounter at a specific location (Nuzlocke rule: only one Pokemon can be caught per route/location)")]
    public static async Task<string> RecordEncounter(
        IStateManager stateManager,
        string? sessionId,
        [Description("Name of the location/route (e.g., 'Route 1', 'Viridian Forest')")] string location,
        [Description("Species caught at this location (null if none caught) ")] string? capturedSpecies = null,
        [Description("Nickname given to captured Pokemon (null if none caught) ")] string? capturedNickname = null)
    {
        try
        {
            var success = string.IsNullOrEmpty(sessionId)
                ? await stateManager.RecordEncounterAsync(
                    location,
                    capturedSpecies?.ToLower(),
                    capturedNickname)
                : await stateManager.RecordEncounterAsync(
                    sessionId,
                    location,
                    capturedSpecies?.ToLower(),
                    capturedNickname);

            if (success)
            {
                return JsonSerializer.Serialize(new
                {
                    success = true,
                    message = capturedSpecies != null
                        ? $"Encounter recorded: caught {capturedSpecies} at {location}"
                        : $"Encounter recorded: no capture at {location}",
                    location,
                    captured = capturedSpecies != null
                }, _jsonOptions);
            }
            else
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Encounter already recorded for {location}. Nuzlocke rule: only one Pokemon per location"
                }, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { success = false, error = ex.Message }, _jsonOptions);
        }
    }
}
