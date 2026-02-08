using es.vargontoc.nuzlocke.ai.Configuration;
using es.vargontoc.nuzlocke.ai.Data;
using es.vargontoc.nuzlocke.ai.Repositories;
using es.vargontoc.nuzlocke.ai.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests;

public class CachedPokeApiConnectorTests : IDisposable
{
    private readonly PokeDbContext _context;
    private readonly CachedPokeApiConnector _cachedConnector;

    public CachedPokeApiConnectorTests()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<PokeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PokeDbContext(options);
        _context.Database.EnsureCreated();

        // Create repositories
        var pokemonRepo = new PokemonCacheRepository(_context);
        var moveRepo = new MoveCacheRepository(_context);
        var typeRepo = new TypeCacheRepository(_context);
        var abilityRepo = new AbilityCacheRepository(_context);

        // Create direct API connector
        var httpClient = new HttpClient();
        var apiLogger = NullLogger<PokeApiConnector>.Instance;
        var pokeApiOptions = Options.Create(new PokeApiOptions());
        var apiConnector = new PokeApiConnector(httpClient, apiLogger, pokeApiOptions);

        // Create cached connector
        var cachedLogger = NullLogger<CachedPokeApiConnector>.Instance;
        _cachedConnector = new CachedPokeApiConnector(
            apiConnector, pokemonRepo, moveRepo, typeRepo, abilityRepo, cachedLogger);
    }

    [Fact]
    public async Task GetPokemonAsync_FirstCall_FetchesFromApiAndCaches()
    {
        // Act - First call should fetch from API
        var result = await _cachedConnector.GetPokemonAsync("pikachu");

        // Assert - Should return valid data
        Assert.NotNull(result);
        Assert.Equal("pikachu", result.Name);
        Assert.Equal(25, result.Id);

        // Verify it was cached
        var cached = await _context.CachedPokemons.FirstOrDefaultAsync(p => p.NameOrId == "pikachu");
        Assert.NotNull(cached);
        Assert.Contains("pikachu", cached.JsonData);
    }

    [Fact]
    public async Task GetPokemonAsync_SecondCall_UsesCache()
    {
        // Arrange - First call to populate cache
        await _cachedConnector.GetPokemonAsync("bulbasaur");

        // Act - Second call should use cache
        var result = await _cachedConnector.GetPokemonAsync("bulbasaur");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("bulbasaur", result.Name);
        Assert.Equal(1, result.Id);

        // Verify cache was used (only one entry should exist)
        var cacheCount = await _context.CachedPokemons.CountAsync(p => p.NameOrId == "bulbasaur");
        Assert.Equal(1, cacheCount);
    }

    [Fact]
    public async Task GetMoveAsync_CachesCorrectly()
    {
        // Act
        var result = await _cachedConnector.GetMoveAsync("thunderbolt");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("thunderbolt", result.Name);

        // Verify cached
        var cached = await _context.CachedMoves.FirstOrDefaultAsync(m => m.NameOrId == "thunderbolt");
        Assert.NotNull(cached);
    }

    [Fact]
    public async Task GetTypeAsync_CachesCorrectly()
    {
        // Act
        var result = await _cachedConnector.GetTypeAsync("electric");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("electric", result.Name);

        // Verify cached
        var cached = await _context.CachedTypes.FirstOrDefaultAsync(t => t.NameOrId == "electric");
        Assert.NotNull(cached);
    }

    [Fact]
    public async Task GetAbilityAsync_CachesCorrectly()
    {
        // Act
        var result = await _cachedConnector.GetAbilityAsync("overgrow");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("overgrow", result.Name);

        // Verify cached
        var cached = await _context.CachedAbilities.FirstOrDefaultAsync(a => a.NameOrId == "overgrow");
        Assert.NotNull(cached);
    }

    [Fact]
    public async Task GetPokemonAsync_InvalidName_DoesNotCache()
    {
        // Act
        var result = await _cachedConnector.GetPokemonAsync("invalidpokemon999");

        // Assert
        Assert.Null(result);

        // Verify not cached
        var cached = await _context.CachedPokemons.FirstOrDefaultAsync(p => p.NameOrId == "invalidpokemon999");
        Assert.Null(cached);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
