using System.Text.Json;
using es.vargontoc.nuzlocke.ai.Data;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Repositories;

namespace es.vargontoc.nuzlocke.ai.Connectors.Impl;

public class CachedPokeApiConnector : IPokeApiConnector
{
    private readonly IPokeApiConnector _apiConnector;
    private readonly ICacheRepository<CachedPokemon> _pokemonCache;
    private readonly ICacheRepository<CachedMove> _moveCache;
    private readonly ICacheRepository<CachedType> _typeCache;
    private readonly ICacheRepository<CachedAbility> _abilityCache;
    private readonly ICacheRepository<CachedItem> _itemCache;
    private readonly ILogger<CachedPokeApiConnector> _logger;

    public CachedPokeApiConnector(
        PokeApiConnector apiConnector,
        ICacheRepository<CachedPokemon> pokemonCache,
        ICacheRepository<CachedMove> moveCache,
        ICacheRepository<CachedType> typeCache,
        ICacheRepository<CachedAbility> abilityCache,
        ICacheRepository<CachedItem> itemCache,
        ILogger<CachedPokeApiConnector> logger)
    {
        _apiConnector = apiConnector;
        _pokemonCache = pokemonCache;
        _moveCache = moveCache;
        _typeCache = typeCache;
        _abilityCache = abilityCache;
        _itemCache = itemCache;
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
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var pokemon = await _apiConnector.GetPokemonAsync(nameOrId);
        sw.Stop();
        _logger.LogInformation("Fetched Pokemon '{NameOrId}' from PokeApi in {Ms}ms", nameOrId, sw.Elapsed.TotalMilliseconds);

        // 3. If found, cache it
        if (pokemon != null)
        {
            var jsonData = JsonSerializer.Serialize(pokemon);
            await _pokemonCache.AddAsync(nameOrId, jsonData);
            _logger.LogInformation("Pokemon '{NameOrId}' cached successfully (size={Bytes} bytes)", nameOrId, System.Text.Encoding.UTF8.GetByteCount(jsonData));
        }

        return pokemon;
    }

    public async Task<PokemonSubset?> GetPokemonSubsetAsync(string nameOrId, int movesLimit = 6)
    {
        // Try cache first
        var cached = await _pokemonCache.GetByNameOrIdAsync(nameOrId);
        PokemonData? full = null;
        if (cached != null)
        {
            _logger.LogInformation("Pokemon '{NameOrId}' found in cache (subset requested)", nameOrId);
            try
            {
                full = JsonSerializer.Deserialize<PokemonData>(cached.JsonData);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached Pokemon json for {NameOrId}", nameOrId);
            }
        }

        if (full == null)
        {
            _logger.LogInformation("Pokemon '{NameOrId}' not in cache (subset), fetching from PokeApi", nameOrId);
            full = await _apiConnector.GetPokemonAsync(nameOrId);
            if (full != null)
            {
                var jsonData = JsonSerializer.Serialize(full);
                await _pokemonCache.AddAsync(nameOrId, jsonData);
            }
        }

        if (full == null) return null;

        var subset = new PokemonSubset
        {
            Id = full.Id,
            Name = full.Name,
            Types = full.Types.Select(t => t.Type.Name).ToList(),
            MovesBasicos = full.Moves.Take(movesLimit).Select(m => m.Move.Name).ToList()
        };

        foreach (var s in full.Stats)
        {
            subset.Stats[s.Stat.Name] = s.BaseStat;
        }

        subset.SpriteMin = null;

        return subset;
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
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var move = await _apiConnector.GetMoveAsync(nameOrId);
        sw.Stop();
        _logger.LogInformation("Fetched Move '{NameOrId}' from PokeApi in {Ms}ms", nameOrId, sw.Elapsed.TotalMilliseconds);

        if (move != null)
        {
            var jsonData = JsonSerializer.Serialize(move);
            await _moveCache.AddAsync(nameOrId, jsonData);
            _logger.LogInformation("Move '{NameOrId}' cached successfully (size={Bytes} bytes)", nameOrId, System.Text.Encoding.UTF8.GetByteCount(jsonData));
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
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var type = await _apiConnector.GetTypeAsync(nameOrId);
        sw.Stop();
        _logger.LogInformation("Fetched Type '{NameOrId}' from PokeApi in {Ms}ms", nameOrId, sw.Elapsed.TotalMilliseconds);

        if (type != null)
        {
            var jsonData = JsonSerializer.Serialize(type);
            await _typeCache.AddAsync(nameOrId, jsonData);
            _logger.LogInformation("Type '{NameOrId}' cached successfully (size={Bytes} bytes)", nameOrId, System.Text.Encoding.UTF8.GetByteCount(jsonData));
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
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var ability = await _apiConnector.GetAbilityAsync(nameOrId);
        sw.Stop();
        _logger.LogInformation("Fetched Ability '{NameOrId}' from PokeApi in {Ms}ms", nameOrId, sw.Elapsed.TotalMilliseconds);

        if (ability != null)
        {
            var jsonData = JsonSerializer.Serialize(ability);
            await _abilityCache.AddAsync(nameOrId, jsonData);
            _logger.LogInformation("Ability '{NameOrId}' cached successfully (size={Bytes} bytes)", nameOrId, System.Text.Encoding.UTF8.GetByteCount(jsonData));
        }

        return ability;
    }

    public async Task<ItemData?> GetItemAsync(string nameOrId)
    {
        var cached = await _itemCache.GetByNameOrIdAsync(nameOrId);
        if (cached != null)
        {
            _logger.LogInformation("Item '{NameOrId}' found in cache", nameOrId);
            return JsonSerializer.Deserialize<ItemData>(cached.JsonData);
        }
        _logger.LogInformation("Item '{NameOrId}' not in cache, fetching from PokeApi", nameOrId);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var item = await _apiConnector.GetItemAsync(nameOrId);
        sw.Stop();
        _logger.LogInformation("Fetched Item '{NameOrId}' from PokeApi in {Ms}ms", nameOrId, sw.Elapsed.TotalMilliseconds);

        if (item != null)
        {
            var jsonData = JsonSerializer.Serialize(item);
            await _itemCache.AddAsync(nameOrId, jsonData);
            _logger.LogInformation("Item '{NameOrId}' cached successfully (size={Bytes} bytes)", nameOrId, System.Text.Encoding.UTF8.GetByteCount(jsonData));
        }

        return item;
    }
}
