using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Services;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

namespace es.vargontoc.nuzlocke.ai.Plugins;

/// <summary>
/// Semantic Kernel plugin for Nuzlocke game state management
/// </summary>
public class NuzlockePlugin
{
    private readonly IStateManager _stateManager;

    public NuzlockePlugin(IStateManager stateManager)
    {
        _stateManager = stateManager;
    }

    [KernelFunction("get_game_state")]
    [Description("Get the current Nuzlocke game state including team, PC storage, deaths, and encounters")]
    [return: Description("JSON containing current team, PC storage, dead Pokemon, and used encounters")]
    public async Task<string> GetGameStateAsync()
    {
        var state = await _stateManager.GetStateAsync();
        return JsonSerializer.Serialize(new
        {
            team = state.Team,
            pcStorage = state.PCStorage,
            deadPokemon = state.DeadPokemon,
            encounters = state.Encounters
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    [KernelFunction("add_to_team")]
    [Description("Add a new Pokemon to the active team (max 6)")]
    [return: Description("Success message or error")]
    public async Task<string> AddToTeamAsync(
        [Description("Pokemon species name (e.g., 'pikachu', 'charizard')")] string species,
        [Description("Nickname given to the Pokemon")] string nickname,
        [Description("Pokemon level")] int level,
        [Description("Comma-separated list of moves")] string? moves = null,
        [Description("Current HP")] int? currentHp = null,
        [Description("Maximum HP")] int? maxHp = null)
    {
        var state = await _stateManager.GetStateAsync();

        if (state.Team.Count >= 6)
        {
            return JsonSerializer.Serialize(new { error = "Team is full (max 6 Pokemon)" });
        }

        var pokemon = new TeamMember
        {
            Species = species,
            Nickname = nickname,
            Level = level,
            Moves = moves?.Split(',').Select(m => m.Trim()).ToList() ?? new List<string>(),
            CurrentHP = currentHp ?? 0,
            MaxHP = maxHp ?? 0
        };

        state.Team.Add(pokemon);
        await _stateManager.SaveStateAsync(state);

        return JsonSerializer.Serialize(new { success = true, message = $"Added {nickname} to team" });
    }

    [KernelFunction("mark_as_dead")]
    [Description("Mark a Pokemon as dead/fainted (permanent in Nuzlocke rules)")]
    [return: Description("Success message or error")]
    public async Task<string> MarkAsDeadAsync(
        [Description("Nickname of the Pokemon that died")] string nickname,
        [Description("Location where the Pokemon died")] string location,
        [Description("Cause of death description")] string causeOfDeath)
    {
        var state = await _stateManager.GetStateAsync();

        var pokemon = state.Team.FirstOrDefault(p => p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));
        if (pokemon == null)
        {
            return JsonSerializer.Serialize(new { error = $"Pokemon '{nickname}' not found in team" });
        }

        // Create dead Pokemon record
        var deadPokemon = new DeadPokemon
        {
            Nickname = pokemon.Nickname,
            Species = pokemon.Species,
            Level = pokemon.Level,
            DeathLocation = location,
            CauseOfDeath = causeOfDeath,
            CaughtAt = pokemon.CaughtAt,
            DeathDate = DateTime.UtcNow
        };

        state.DeadPokemon.Add(deadPokemon);
        state.Team.Remove(pokemon);

        await _stateManager.SaveStateAsync(state);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"RIP {nickname}. Died at {location}: {causeOfDeath}"
        });
    }

    [KernelFunction("move_to_pc")]
    [Description("Move a Pokemon from the active team to PC storage")]
    [return: Description("Success message or error")]
    public async Task<string> MoveToPcAsync(
        [Description("Nickname of the Pokemon to move to PC")] string nickname)
    {
        var state = await _stateManager.GetStateAsync();

        var pokemon = state.Team.FirstOrDefault(p => p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));
        if (pokemon == null)
        {
            return JsonSerializer.Serialize(new { error = $"Pokemon '{nickname}' not found in team" });
        }

        // Create stored Pokemon record
        var storedPokemon = new StoredPokemon
        {
            Nickname = pokemon.Nickname,
            Species = pokemon.Species,
            Level = pokemon.Level,
            Moves = pokemon.Moves,
            CaughtAt = pokemon.CaughtAt,
            CaughtDate = pokemon.CaughtDate
        };

        state.PCStorage.Add(storedPokemon);
        state.Team.Remove(pokemon);

        await _stateManager.SaveStateAsync(state);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Moved {nickname} to PC storage"
        });
    }

    [KernelFunction("record_encounter")]
    [Description("Record a route/area encounter (Nuzlocke rule: only first Pokemon per route can be caught)")]
    [return: Description("Success message confirming the encounter was recorded")]
    public async Task<string> RecordEncounterAsync(
        [Description("Location/route name where the encounter happened")] string location,
        [Description("Pokemon species encountered")] string? capturedSpecies = null,
        [Description("Nickname given to captured Pokemon")] string? capturedNickname = null)
    {
        var state = await _stateManager.GetStateAsync();

        if (state.Encounters.ContainsKey(location))
        {
            return JsonSerializer.Serialize(new
            {
                error = $"Encounter already used at {location}",
                previous = state.Encounters[location]
            });
        }

        state.Encounters[location] = new EncounterRecord
        {
            Location = location,
            EncounterUsed = !string.IsNullOrEmpty(capturedSpecies),
            CapturedSpecies = capturedSpecies,
            CapturedNickname = capturedNickname,
            EncounterDate = DateTime.UtcNow
        };

        await _stateManager.SaveStateAsync(state);

        var message = !string.IsNullOrEmpty(capturedSpecies)
            ? $"Recorded encounter at {location}: Caught {capturedNickname} ({capturedSpecies})"
            : $"Recorded failed/skipped encounter at {location}";

        return JsonSerializer.Serialize(new { success = true, message });
    }
}
