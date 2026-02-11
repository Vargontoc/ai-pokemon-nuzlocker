using System.Text.Json;
using es.vargontoc.nuzlocke.ai.Configuration;
using es.vargontoc.nuzlocke.ai.Data;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace es.vargontoc.nuzlocke.ai.Connectors.Impl;

public class CachedPokeApiConnector : IPokeApiConnector
{
    private readonly IPokeApiConnector _apiConnector;
    private readonly ICacheRepository<CachedPokemon> _pokemonCache;
    private readonly ICacheRepository<CachedMove> _moveCache;
    private readonly ICacheRepository<CachedType> _typeCache;
    private readonly ICacheRepository<CachedAbility> _abilityCache;
    private readonly ICacheRepository<CachedItem> _itemCache;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<CachedPokeApiConnector> _logger;
    private readonly TimeSpan _ttl;

    public CachedPokeApiConnector(
        IPokeApiConnector apiConnector,
        ICacheRepository<CachedPokemon> pokemonCache,
        ICacheRepository<CachedMove> moveCache,
        ICacheRepository<CachedType> typeCache,
        ICacheRepository<CachedAbility> abilityCache,
        ICacheRepository<CachedItem> itemCache,
        IMemoryCache memoryCache,
        ILogger<CachedPokeApiConnector> logger,
        IOptions<PokeApiOptions> options)
    {
        _apiConnector = apiConnector;
        _pokemonCache = pokemonCache;
        _moveCache = moveCache;
        _typeCache = typeCache;
        _abilityCache = abilityCache;
        _itemCache = itemCache;
        _memoryCache = memoryCache;
        _logger = logger;
        _ttl = TimeSpan.FromMinutes(options.Value.MemoryCacheTtlMinutes);
    }

    public Task<PokemonData?> GetPokemonAsync(string nameOrId)
    {
        return GetOrFetchAsync<PokemonData, CachedPokemon>(
            "Pokemon", nameOrId, _pokemonCache,
            _apiConnector.GetPokemonAsync,
            cached => cached.JsonData,
            _pokemonCache.AddAsync);
    }

    public async Task<PokemonSubset?> GetPokemonSubsetAsync(string nameOrId, int movesLimit = 6)
    {
        var full = await GetPokemonAsync(nameOrId);
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

    public Task<MoveData?> GetMoveAsync(string nameOrId)
    {
        return GetOrFetchAsync<MoveData, CachedMove>(
            "Move", nameOrId, _moveCache,
            _apiConnector.GetMoveAsync,
            cached => cached.JsonData,
            _moveCache.AddAsync);
    }

    public Task<TypeData?> GetTypeAsync(string nameOrId)
    {
        return GetOrFetchAsync<TypeData, CachedType>(
            "Type", nameOrId, _typeCache,
            _apiConnector.GetTypeAsync,
            cached => cached.JsonData,
            _typeCache.AddAsync);
    }

    public Task<AbilityData?> GetAbilityAsync(string nameOrId)
    {
        return GetOrFetchAsync<AbilityData, CachedAbility>(
            "Ability", nameOrId, _abilityCache,
            _apiConnector.GetAbilityAsync,
            cached => cached.JsonData,
            _abilityCache.AddAsync);
    }

    public Task<ItemData?> GetItemAsync(string nameOrId)
    {
        return GetOrFetchAsync<ItemData, CachedItem>(
            "Item", nameOrId, _itemCache,
            _apiConnector.GetItemAsync,
            cached => cached.JsonData,
            _itemCache.AddAsync);
    }

    private async Task<TData?> GetOrFetchAsync<TData, TCached>(
        string entityType,
        string nameOrId,
        ICacheRepository<TCached> l2Cache,
        Func<string, Task<TData?>> apiFetch,
        Func<TCached, string> getJsonData,
        Func<string, string, Task> l2Store)
        where TData : class
        where TCached : class
    {
        var cacheKey = $"{entityType}:{nameOrId.ToLower()}";

        // L1: Memory cache lookup
        if (_ttl > TimeSpan.Zero && _memoryCache.TryGetValue(cacheKey, out TData? memoryCached))
        {
            _logger.LogInformation(
                "Cache {CacheResult} on {CacheLayer} for {EntityType} '{NameOrId}'",
                "Hit", "L1_Memory", entityType, nameOrId);
            return memoryCached;
        }

        if (_ttl > TimeSpan.Zero)
        {
            _logger.LogInformation(
                "Cache {CacheResult} on {CacheLayer} for {EntityType} '{NameOrId}'",
                "Miss", "L1_Memory", entityType, nameOrId);
        }

        // L2: SQLite cache lookup
        var l2Entry = await l2Cache.GetByNameOrIdAsync(nameOrId);
        if (l2Entry != null)
        {
            _logger.LogInformation(
                "Cache {CacheResult} on {CacheLayer} for {EntityType} '{NameOrId}'",
                "Hit", "L2_SQLite", entityType, nameOrId);

            var jsonData = getJsonData(l2Entry);
            var deserialized = JsonSerializer.Deserialize<TData>(jsonData);

            // Promote to L1
            if (deserialized != null && _ttl > TimeSpan.Zero)
            {
                _memoryCache.Set(cacheKey, deserialized, new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(_ttl));
            }

            return deserialized;
        }

        _logger.LogInformation(
            "Cache {CacheResult} on {CacheLayer} for {EntityType} '{NameOrId}'",
            "Miss", "L2_SQLite", entityType, nameOrId);

        // L3: Fetch from PokeAPI
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await apiFetch(nameOrId);
        sw.Stop();
        _logger.LogInformation(
            "Fetched {EntityType} '{NameOrId}' from PokeApi in {ElapsedMs}ms",
            entityType, nameOrId, sw.Elapsed.TotalMilliseconds);

        if (result != null)
        {
            var serialized = JsonSerializer.Serialize(result);

            // Store in L2 (SQLite)
            await l2Store(nameOrId, serialized);

            // Store in L1 (Memory)
            if (_ttl > TimeSpan.Zero)
            {
                _memoryCache.Set(cacheKey, result, new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(_ttl));
            }

            _logger.LogInformation(
                "{EntityType} '{NameOrId}' cached in L1+L2 (size={Bytes} bytes)",
                entityType, nameOrId, System.Text.Encoding.UTF8.GetByteCount(serialized));
        }

        return result;
    }
}
