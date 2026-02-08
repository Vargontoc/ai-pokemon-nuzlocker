using System.Text.Json;
using es.vargontoc.nuzlocke.ai.Data;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Repositories;

namespace es.vargontoc.nuzlocke.ai.Services;

public class CachedPokeApiConnector : IPokeApiConnector
{
    private readonly IPokeApiConnector _apiConnector;
    private readonly ICacheRepository<CachedPokemon> _pokemonCache;
    private readonly ICacheRepository<CachedMove> _moveCache;
    private readonly ICacheRepository<CachedType> _typeCache;
    private readonly ICacheRepository<CachedAbility> _abilityCache;
    private readonly ILogger<CachedPokeApiConnector> _logger;

    public CachedPokeApiConnector(
        PokeApiConnector apiConnector,
        ICacheRepository<CachedPokemon> pokemonCache,
        ICacheRepository<CachedMove> moveCache,
        ICacheRepository<CachedType> typeCache,
        ICacheRepository<CachedAbility> abilityCache,
        ILogger<CachedPokeApiConnector> logger)
    {
        _apiConnector = apiConnector;
        _pokemonCache = pokemonCache;
        _moveCache = moveCache;
        _typeCache = typeCache;
        _abilityCache = abilityCache;
        _logger = logger;
    }

    public async Task<PokemonData?> GetPokemonAsync(string nameOrId)
    {
        // 1. Try SQLite cache first
        var cached = await _pokemonCache.GetByNameOrIdAsync(nameOrId);
        if (cached != null)
        {
            _logger.LogInformation("Pokemon '{NameOrId}' found in cache", nameOrId);
            return JsonSerializer.Deserialize<PokemonData>(cached.JsonData);
        }

        // 2. Not in cache, fetch from PokeApi
        _logger.LogInformation("Pokemon '{NameOrId}' not in cache, fetching from PokeApi", nameOrId);
        var pokemon = await _apiConnector.GetPokemonAsync(nameOrId);

        // 3. If found, cache it
        if (pokemon != null)
        {
            var jsonData = JsonSerializer.Serialize(pokemon);
            await _pokemonCache.AddAsync(nameOrId, jsonData);
            _logger.LogInformation("Pokemon '{NameOrId}' cached successfully", nameOrId);
        }

        return pokemon;
    }

    public async Task<MoveData?> GetMoveAsync(string nameOrId)
    {
        var cached = await _moveCache.GetByNameOrIdAsync(nameOrId);
        if (cached != null)
        {
            _logger.LogInformation("Move '{NameOrId}' found in cache", nameOrId);
            return JsonSerializer.Deserialize<MoveData>(cached.JsonData);
        }

        _logger.LogInformation("Move '{NameOrId}' not in cache, fetching from PokeApi", nameOrId);
        var move = await _apiConnector.GetMoveAsync(nameOrId);

        if (move != null)
        {
            var jsonData = JsonSerializer.Serialize(move);
            await _moveCache.AddAsync(nameOrId, jsonData);
            _logger.LogInformation("Move '{NameOrId}' cached successfully", nameOrId);
        }

        return move;
    }

    public async Task<TypeData?> GetTypeAsync(string nameOrId)
    {
        var cached = await _typeCache.GetByNameOrIdAsync(nameOrId);
        if (cached != null)
        {
            _logger.LogInformation("Type '{NameOrId}' found in cache", nameOrId);
            return JsonSerializer.Deserialize<TypeData>(cached.JsonData);
        }

        _logger.LogInformation("Type '{NameOrId}' not in cache, fetching from PokeApi", nameOrId);
        var type = await _apiConnector.GetTypeAsync(nameOrId);

        if (type != null)
        {
            var jsonData = JsonSerializer.Serialize(type);
            await _typeCache.AddAsync(nameOrId, jsonData);
            _logger.LogInformation("Type '{NameOrId}' cached successfully", nameOrId);
        }

        return type;
    }

    public async Task<AbilityData?> GetAbilityAsync(string nameOrId)
    {
        var cached = await _abilityCache.GetByNameOrIdAsync(nameOrId);
        if (cached != null)
        {
            _logger.LogInformation("Ability '{NameOrId}' found in cache", nameOrId);
            return JsonSerializer.Deserialize<AbilityData>(cached.JsonData);
        }

        _logger.LogInformation("Ability '{NameOrId}' not in cache, fetching from PokeApi", nameOrId);
        var ability = await _apiConnector.GetAbilityAsync(nameOrId);

        if (ability != null)
        {
            var jsonData = JsonSerializer.Serialize(ability);
            await _abilityCache.AddAsync(nameOrId, jsonData);
            _logger.LogInformation("Ability '{NameOrId}' cached successfully", nameOrId);
        }

        return ability;
    }
}
