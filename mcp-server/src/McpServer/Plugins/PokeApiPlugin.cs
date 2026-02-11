using es.vargontoc.nuzlocke.ai.Connectors;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

namespace es.vargontoc.nuzlocke.ai.Plugins;

/// <summary>
/// Semantic Kernel plugin for PokeAPI data access
/// </summary>
public class PokeApiPlugin(ILogger<PokeApiPlugin> _logger, IPokeApiConnector _pokeApiConnector)
{

    [KernelFunction("get_pokemon")]
    [Description("Get detailed information about a Pokemon including stats, types, abilities, and moves. Use this to analyze Pokemon for strategic decisions.")]
    [return: Description("JSON with Pokemon data including stats, types, abilities, and available moves")]
    public async Task<string> GetPokemonAsync(
        [Description("Pokemon name or ID (e.g., 'pikachu', 'charizard', or '25')")] string nameOrId)
    {
        try
        {
            _logger.LogInformation("GetPokemonAsync called with {nameOrId}", nameOrId);
            var pokemon = await _pokeApiConnector.GetPokemonAsync(nameOrId.ToLower());
            if (pokemon == null)
            {
                _logger.LogWarning("Pokemon not found: {nameOrId}", nameOrId);
                return JsonSerializer.Serialize(new { error = $"Pokemon '{nameOrId}' not found" });
            }

            var payload = JsonSerializer.Serialize(new
            {
                id = pokemon.Id,
                name = pokemon.Name,
                types = pokemon.Types.Select(t => t.Type.Name),
                stats = pokemon.Stats.Select(s => new
                {
                    name = s.Stat.Name,
                    baseStat = s.BaseStat,
                    effort = s.Effort
                }),
                abilities = pokemon.Abilities.Select(a => new
                {
                    name = a.Ability.Name,
                    isHidden = a.IsHidden
                }),
                height = pokemon.Height,
                weight = pokemon.Weight,
                baseExperience = pokemon.BaseExperience
            }, new JsonSerializerOptions { WriteIndented = true });

            _logger.LogDebug("GetPokemonAsync returning payload length {len} for {nameOrId}", payload?.Length ?? 0, nameOrId);
            return payload!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPokemonAsync for {nameOrId}", nameOrId);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [KernelFunction("get_move")]
    [Description("Get detailed information about a move including power, accuracy, type, and effects. Use this to evaluate move effectiveness.")]
    [return: Description("JSON with move data including power, accuracy, type, PP, and effect description")]
    public async Task<string> GetMoveAsync(
        [Description("Move name or ID (e.g., 'thunderbolt', 'surf', or '85')")] string nameOrId)
    {
        try
        {
            _logger.LogInformation("GetMoveAsync called with {nameOrId}", nameOrId);
            var move = await _pokeApiConnector.GetMoveAsync(nameOrId.ToLower());
            if (move == null)
            {
                _logger.LogWarning("Move not found: {nameOrId}", nameOrId);
                return JsonSerializer.Serialize(new { error = $"Move '{nameOrId}' not found" });
            }

            var payload = JsonSerializer.Serialize(new
            {
                id = move.Id,
                name = move.Name,
                type = move.Type.Name,
                damageClass = move.DamageClass.Name,
                power = move.Power,
                accuracy = move.Accuracy,
                pp = move.Pp,
                priority = move.Priority,
                effect = move.EffectEntries.FirstOrDefault(e => e.Language.Name == "en")?.ShortEffect
            }, new JsonSerializerOptions { WriteIndented = true });

            _logger.LogDebug("GetMoveAsync returning payload length {len} for {nameOrId}", payload?.Length ?? 0, nameOrId);
            return payload!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetMoveAsync for {nameOrId}", nameOrId);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [KernelFunction("get_type")]
    [Description("Get type effectiveness information including super effective, not very effective, and immune matchups. Use this for battle strategy.")]
    [return: Description("JSON with type matchup data for offensive and defensive capabilities")]
    public async Task<string> GetTypeAsync(
        [Description("Type name or ID (e.g., 'fire', 'water', 'psychic', or '10')")] string nameOrId)
    {
        try
        {
            _logger.LogInformation("GetTypeAsync called with {nameOrId}", nameOrId);
            var typeData = await _pokeApiConnector.GetTypeAsync(nameOrId.ToLower());
            if (typeData == null)
            {
                _logger.LogWarning("Type not found: {nameOrId}", nameOrId);
                return JsonSerializer.Serialize(new { error = $"Type '{nameOrId}' not found" });
            }

            var payload = JsonSerializer.Serialize(new
            {
                id = typeData.Id,
                name = typeData.Name,
                doubleDamageTo = typeData.DamageRelations.DoubleDamageTo.Select(t => t.Name),
                halfDamageTo = typeData.DamageRelations.HalfDamageTo.Select(t => t.Name),
                noDamageTo = typeData.DamageRelations.NoDamageTo.Select(t => t.Name),
                doubleDamageFrom = typeData.DamageRelations.DoubleDamageFrom.Select(t => t.Name),
                halfDamageFrom = typeData.DamageRelations.HalfDamageFrom.Select(t => t.Name),
                noDamageFrom = typeData.DamageRelations.NoDamageFrom.Select(t => t.Name)
            }, new JsonSerializerOptions { WriteIndented = true });

            _logger.LogDebug("GetTypeAsync returning payload length {len} for {nameOrId}", payload?.Length ?? 0, nameOrId);
            return payload!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetTypeAsync for {nameOrId}", nameOrId);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [KernelFunction("get_ability")]
    [Description("Get detailed information about an ability including effects and description. Use this to understand Pokemon abilities.")]
    [return: Description("JSON with ability data including name, effect description, and generation introduced")]
    public async Task<string> GetAbilityAsync(
        [Description("Ability name or ID (e.g., 'overgrow', 'torrent', or '65')")] string nameOrId)
    {
        try
        {
            _logger.LogInformation("GetAbilityAsync called with {nameOrId}", nameOrId);
            var ability = await _pokeApiConnector.GetAbilityAsync(nameOrId.ToLower());
            if (ability == null)
            {
                _logger.LogWarning("Ability not found: {nameOrId}", nameOrId);
                return JsonSerializer.Serialize(new { error = $"Ability '{nameOrId}' not found" });
            }

            var payload = JsonSerializer.Serialize(new
            {
                id = ability.Id,
                name = ability.Name,
                effect = ability.EffectEntries.FirstOrDefault(e => e.Language.Name == "en")?.Effect,
                shortEffect = ability.EffectEntries.FirstOrDefault(e => e.Language.Name == "en")?.ShortEffect
            }, new JsonSerializerOptions { WriteIndented = true });

            _logger.LogDebug("GetAbilityAsync returning payload length {len} for {nameOrId}", payload?.Length ?? 0, nameOrId);
            return payload!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAbilityAsync for {nameOrId}", nameOrId);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [KernelFunction("get_item")]
    [Description("Get detailed information about an item including category, effects, cost, and sprite. Useful for pokeballs, medicines, berries, and held items.")]
    [return: Description("JSON with item data including name, category, cost, effect description, and sprite URL")]
    public async Task<string> GetItemAsync(
        [Description("Item name or ID (e.g., 'poke-ball', 'potion', 'oran-berry', or '1')")] string nameOrId)
    {
        try
        {
            _logger.LogInformation("GetItemAsync called with {nameOrId}", nameOrId);
            var item = await _pokeApiConnector.GetItemAsync(nameOrId.ToLower());
            if (item == null)
            {
                _logger.LogWarning("Item not found: {nameOrId}", nameOrId);
                return JsonSerializer.Serialize(new { error = $"Item '{nameOrId}' not found" });
            }

            var payload = JsonSerializer.Serialize(new
            {
                id = item.Id,
                name = item.Name,
                cost = item.Cost,
                category = item.Category.Name,
                effect = item.EffectEntries.FirstOrDefault(e => e.Language.Name == "en")?.Effect,
                shortEffect = item.EffectEntries.FirstOrDefault(e => e.Language.Name == "en")?.ShortEffect
            }, new JsonSerializerOptions { WriteIndented = true });

            _logger.LogDebug("GetItemAsync returning payload length {len} for {nameOrId}", payload?.Length ?? 0, nameOrId);
            return payload!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetItemAsync for {nameOrId}", nameOrId);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
