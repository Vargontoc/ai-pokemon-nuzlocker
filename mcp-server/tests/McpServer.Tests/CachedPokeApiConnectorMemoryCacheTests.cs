using System.Text.Json;
using es.vargontoc.nuzlocke.ai.Configuration;
using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Connectors.Impl;
using es.vargontoc.nuzlocke.ai.Data;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests;

public class CachedPokeApiConnectorMemoryCacheTests : IDisposable
{
    private readonly Mock<IPokeApiConnector> _mockApiConnector;
    private readonly Mock<ICacheRepository<CachedPokemon>> _mockPokemonRepo;
    private readonly Mock<ICacheRepository<CachedMove>> _mockMoveRepo;
    private readonly Mock<ICacheRepository<CachedType>> _mockTypeRepo;
    private readonly Mock<ICacheRepository<CachedAbility>> _mockAbilityRepo;
    private readonly Mock<ICacheRepository<CachedItem>> _mockItemRepo;
    private readonly MemoryCache _memoryCache;

    public CachedPokeApiConnectorMemoryCacheTests()
    {
        _mockApiConnector = new Mock<IPokeApiConnector>();
        _mockPokemonRepo = new Mock<ICacheRepository<CachedPokemon>>();
        _mockMoveRepo = new Mock<ICacheRepository<CachedMove>>();
        _mockTypeRepo = new Mock<ICacheRepository<CachedType>>();
        _mockAbilityRepo = new Mock<ICacheRepository<CachedAbility>>();
        _mockItemRepo = new Mock<ICacheRepository<CachedItem>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
    }

    private CachedPokeApiConnector CreateConnector(int ttlMinutes = 10)
    {
        var options = Options.Create(new PokeApiOptions
        {
            MemoryCacheTtlMinutes = ttlMinutes
        });
        return new CachedPokeApiConnector(
            _mockApiConnector.Object,
            _mockPokemonRepo.Object,
            _mockMoveRepo.Object,
            _mockTypeRepo.Object,
            _mockAbilityRepo.Object,
            _mockItemRepo.Object,
            _memoryCache,
            NullLogger<CachedPokeApiConnector>.Instance,
            options);
    }

    private static PokemonData CreatePokemonData(int id, string name)
    {
        return new PokemonData
        {
            Id = id,
            Name = name,
            Types = new List<PokemonTypeSlot>
            {
                new() { Slot = 1, Type = new NamedApiResource { Name = "electric" } }
            },
            Stats = new List<PokemonStatSlot>
            {
                new() { BaseStat = 35, Effort = 0, Stat = new NamedApiResource { Name = "hp" } }
            },
            Moves = new List<PokemonMoveSlot>
            {
                new() { Move = new NamedApiResource { Name = "thunderbolt" } }
            },
            Abilities = new List<PokemonAbilitySlot>
            {
                new() { Ability = new NamedApiResource { Name = "static" }, IsHidden = false }
            }
        };
    }

    [Fact]
    public async Task GetPokemonAsync_SecondCall_ReturnsFromL1MemoryCache()
    {
        // Arrange: L2 empty, API returns data
        _mockPokemonRepo.Setup(r => r.GetByNameOrIdAsync("pikachu"))
            .ReturnsAsync((CachedPokemon?)null);
        _mockApiConnector.Setup(a => a.GetPokemonAsync("pikachu"))
            .ReturnsAsync(CreatePokemonData(25, "pikachu"));

        var connector = CreateConnector(ttlMinutes: 10);

        // Act: first call populates L1 + L2
        await connector.GetPokemonAsync("pikachu");

        // Act: second call should hit L1
        var result = await connector.GetPokemonAsync("pikachu");

        // Assert: API and L2 called only once (first call)
        Assert.NotNull(result);
        Assert.Equal("pikachu", result.Name);
        _mockApiConnector.Verify(a => a.GetPokemonAsync("pikachu"), Times.Once);
        _mockPokemonRepo.Verify(r => r.GetByNameOrIdAsync("pikachu"), Times.Once);
    }

    [Fact]
    public async Task GetPokemonAsync_L1Miss_L2Hit_PromotesToL1()
    {
        // Arrange: L2 has cached data
        var pokemonData = CreatePokemonData(4, "charmander");
        var cachedEntry = new CachedPokemon
        {
            NameOrId = "charmander",
            JsonData = JsonSerializer.Serialize(pokemonData),
            CachedAt = DateTime.UtcNow
        };
        _mockPokemonRepo.Setup(r => r.GetByNameOrIdAsync("charmander"))
            .ReturnsAsync(cachedEntry);

        var connector = CreateConnector(ttlMinutes: 10);

        // Act: first call - L1 miss, L2 hit, promotes to L1
        var result = await connector.GetPokemonAsync("charmander");

        // Assert: data returned from L2, API never called
        Assert.NotNull(result);
        Assert.Equal("charmander", result.Name);
        _mockApiConnector.Verify(a => a.GetPokemonAsync(It.IsAny<string>()), Times.Never);

        // Act: second call should now hit L1 (no additional L2 query)
        await connector.GetPokemonAsync("charmander");
        _mockPokemonRepo.Verify(r => r.GetByNameOrIdAsync("charmander"), Times.Once);
    }

    [Fact]
    public async Task GetPokemonAsync_AfterTtlExpires_FallsBackToL2()
    {
        // Arrange: L2 has data
        var pokemonData = CreatePokemonData(7, "squirtle");
        var cachedEntry = new CachedPokemon
        {
            NameOrId = "squirtle",
            JsonData = JsonSerializer.Serialize(pokemonData),
            CachedAt = DateTime.UtcNow
        };
        _mockPokemonRepo.Setup(r => r.GetByNameOrIdAsync("squirtle"))
            .ReturnsAsync(cachedEntry);

        var connector = CreateConnector(ttlMinutes: 10);

        // First call: populates L1 from L2
        await connector.GetPokemonAsync("squirtle");

        // Simulate TTL expiration by removing from L1
        _memoryCache.Remove("Pokemon:squirtle");

        // Second call: L1 miss, falls back to L2
        var result = await connector.GetPokemonAsync("squirtle");

        Assert.NotNull(result);
        Assert.Equal("squirtle", result.Name);
        // L2 queried twice (once per L1 miss)
        _mockPokemonRepo.Verify(r => r.GetByNameOrIdAsync("squirtle"), Times.Exactly(2));
        // API never called (L2 always had data)
        _mockApiConnector.Verify(a => a.GetPokemonAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetPokemonAsync_TtlZero_SkipsL1MemoryCache()
    {
        // Arrange: L2 has data
        var pokemonData = CreatePokemonData(1, "bulbasaur");
        var cachedEntry = new CachedPokemon
        {
            NameOrId = "bulbasaur",
            JsonData = JsonSerializer.Serialize(pokemonData),
            CachedAt = DateTime.UtcNow
        };
        _mockPokemonRepo.Setup(r => r.GetByNameOrIdAsync("bulbasaur"))
            .ReturnsAsync(cachedEntry);

        var connector = CreateConnector(ttlMinutes: 0); // L1 disabled

        // Act: two calls
        await connector.GetPokemonAsync("bulbasaur");
        await connector.GetPokemonAsync("bulbasaur");

        // Assert: L2 queried each time (L1 disabled)
        _mockPokemonRepo.Verify(r => r.GetByNameOrIdAsync("bulbasaur"), Times.Exactly(2));
    }

    [Fact]
    public async Task GetPokemonAsync_FullMiss_FetchesFromApiAndStoresInBothLayers()
    {
        // Arrange: L2 empty, API returns data
        _mockPokemonRepo.Setup(r => r.GetByNameOrIdAsync("mewtwo"))
            .ReturnsAsync((CachedPokemon?)null);
        _mockApiConnector.Setup(a => a.GetPokemonAsync("mewtwo"))
            .ReturnsAsync(CreatePokemonData(150, "mewtwo"));

        var connector = CreateConnector(ttlMinutes: 10);

        // Act
        var result = await connector.GetPokemonAsync("mewtwo");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(150, result.Id);
        _mockApiConnector.Verify(a => a.GetPokemonAsync("mewtwo"), Times.Once);
        _mockPokemonRepo.Verify(r => r.AddAsync("mewtwo", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetPokemonAsync_ApiReturnsNull_DoesNotCache()
    {
        // Arrange
        _mockPokemonRepo.Setup(r => r.GetByNameOrIdAsync("fakemon"))
            .ReturnsAsync((CachedPokemon?)null);
        _mockApiConnector.Setup(a => a.GetPokemonAsync("fakemon"))
            .ReturnsAsync((PokemonData?)null);

        var connector = CreateConnector(ttlMinutes: 10);

        // Act
        var result = await connector.GetPokemonAsync("fakemon");

        // Assert
        Assert.Null(result);
        _mockPokemonRepo.Verify(r => r.AddAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetMoveAsync_L1Hit_DoesNotQueryL2()
    {
        // Arrange: L2 empty, API returns data
        _mockMoveRepo.Setup(r => r.GetByNameOrIdAsync("thunderbolt"))
            .ReturnsAsync((CachedMove?)null);
        _mockApiConnector.Setup(a => a.GetMoveAsync("thunderbolt"))
            .ReturnsAsync(new MoveData
            {
                Id = 85,
                Name = "thunderbolt",
                Type = new NamedApiResource { Name = "electric" },
                DamageClass = new NamedApiResource { Name = "special" }
            });

        var connector = CreateConnector(ttlMinutes: 10);

        // Act: first call populates L1
        await connector.GetMoveAsync("thunderbolt");
        // Act: second call hits L1
        await connector.GetMoveAsync("thunderbolt");

        // Assert: L2 and API called only once
        _mockMoveRepo.Verify(r => r.GetByNameOrIdAsync("thunderbolt"), Times.Once);
        _mockApiConnector.Verify(a => a.GetMoveAsync("thunderbolt"), Times.Once);
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
    }
}
