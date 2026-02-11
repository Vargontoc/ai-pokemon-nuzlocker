using es.vargontoc.nuzlocke.ai.Configuration;
using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Connectors.Impl;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests;

public class PokeApiConnectorTests
{
    private readonly IPokeApiConnector _connector;

    public PokeApiConnectorTests()
    {
        var httpClient = new HttpClient();
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<PokeApiConnector>();
        var options = Options.Create(new PokeApiOptions
        {
            BaseUrl = "https://pokeapi.co/api/v2",
            TimeoutSeconds = 30
        });
        _connector = new PokeApiConnector(httpClient, logger, options);
    }

    [Fact]
    public async Task GetPokemonAsync_WithValidName_ReturnsPokemonData()
    {
        // Act
        var result = await _connector.GetPokemonAsync("pikachu");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("pikachu", result.Name);
        Assert.Equal(25, result.Id);
        Assert.NotEmpty(result.Types);
        Assert.NotEmpty(result.Abilities);
        Assert.NotEmpty(result.Stats);
    }

    [Fact]
    public async Task GetPokemonAsync_WithValidId_ReturnsPokemonData()
    {
        // Act
        var result = await _connector.GetPokemonAsync("1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("bulbasaur", result.Name);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetPokemonAsync_WithInvalidName_ReturnsNull()
    {
        // Act
        var result = await _connector.GetPokemonAsync("invalidpokemon123");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMoveAsync_WithValidName_ReturnsMoveData()
    {
        // Act
        var result = await _connector.GetMoveAsync("thunderbolt");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("thunderbolt", result.Name);
        Assert.NotNull(result.Power);
        Assert.True(result.Power > 0);
        Assert.NotNull(result.Accuracy);
    }

    [Fact]
    public async Task GetMoveAsync_WithInvalidName_ReturnsNull()
    {
        // Act
        var result = await _connector.GetMoveAsync("invalidmove123");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTypeAsync_WithValidName_ReturnsTypeData()
    {
        // Act
        var result = await _connector.GetTypeAsync("electric");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("electric", result.Name);
        Assert.NotNull(result.DamageRelations);
        Assert.NotEmpty(result.DamageRelations.DoubleDamageTo);
    }

    [Fact]
    public async Task GetTypeAsync_WithInvalidName_ReturnsNull()
    {
        // Act
        var result = await _connector.GetTypeAsync("invalidtype123");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAbilityAsync_WithValidName_ReturnsAbilityData()
    {
        // Act
        var result = await _connector.GetAbilityAsync("overgrow");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("overgrow", result.Name);
        Assert.NotEmpty(result.EffectEntries);
    }

    [Fact]
    public async Task GetAbilityAsync_WithInvalidName_ReturnsNull()
    {
        // Act
        var result = await _connector.GetAbilityAsync("invalidability123");

        // Assert
        Assert.Null(result);
    }
}
