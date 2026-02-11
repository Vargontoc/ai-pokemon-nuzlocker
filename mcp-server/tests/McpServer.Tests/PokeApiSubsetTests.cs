using es.vargontoc.nuzlocke.ai.Connectors.Impl;
using es.vargontoc.nuzlocke.ai.Configuration;
using es.vargontoc.nuzlocke.ai.Data;
using es.vargontoc.nuzlocke.ai.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests;

public class PokeApiSubsetTests : IDisposable
{
    private readonly PokeDbContext _context;
    private readonly CachedPokeApiConnector _cachedConnector;

    public PokeApiSubsetTests()
    {
        var options = new DbContextOptionsBuilder<PokeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PokeDbContext(options);
        _context.Database.EnsureCreated();

        var pokemonRepo = new PokemonCacheRepository(_context);
        var moveRepo = new MoveCacheRepository(_context);
        var typeRepo = new TypeCacheRepository(_context);
        var abilityRepo = new AbilityCacheRepository(_context);
        var itemRepo = new ItemCacheRepository(_context);

        var httpClient = new HttpClient();
        var apiLogger = NullLogger<PokeApiConnector>.Instance;
        var pokeApiOptions = Options.Create(new PokeApiOptions());
        var apiConnector = new PokeApiConnector(httpClient, apiLogger, pokeApiOptions);

        var cachedLogger = NullLogger<CachedPokeApiConnector>.Instance;
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cachedOptions = Options.Create(new PokeApiOptions { MemoryCacheTtlMinutes = 60 });
        _cachedConnector = new CachedPokeApiConnector(
            apiConnector, pokemonRepo, moveRepo, typeRepo, abilityRepo, itemRepo,
            memoryCache, cachedLogger, cachedOptions);
    }

    [Fact]
    public async Task GetPokemonSubsetAsync_Pikachu_ReturnsExpectedFields()
    {
        var subset = await _cachedConnector.GetPokemonSubsetAsync("pikachu", movesLimit: 4);

        Assert.NotNull(subset);
        Assert.Equal("pikachu", subset.Name);
        Assert.True(subset.Id > 0);
        Assert.NotEmpty(subset.Types);
        Assert.NotEmpty(subset.Stats);
        Assert.True(subset.MovesBasicos.Count <= 4);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
