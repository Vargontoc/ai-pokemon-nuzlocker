using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using es.vargontoc.nuzlocke.ai.Configuration;
using es.vargontoc.nuzlocke.ai.Tools;
using System.Text.Json;

namespace es.vargontoc.nuzlocke.ai.Tests;

[Collection("WebApp collection")]
public class McpEndpointsTests
{
    private readonly WebApplicationFactory<es.vargontoc.nuzlocke.ai.Program> _factory;
    private readonly HttpClient _client;

    public McpEndpointsTests(WebApplicationFactory<es.vargontoc.nuzlocke.ai.Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task McpServer_StartsSuccessfully()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task PokeApiTools_GetPokemon_WorksDirectly()
    {
        // Arrange
        var httpClient = new HttpClient();
        var logger = NullLogger<es.vargontoc.nuzlocke.ai.Services.PokeApiConnector>.Instance;
        var options = Options.Create(new PokeApiOptions());
        var connector = new es.vargontoc.nuzlocke.ai.Services.PokeApiConnector(httpClient, logger, options);

        // Act
        var result = await PokeApiTools.GetPokemon(connector, "pikachu");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("pikachu", result, StringComparison.OrdinalIgnoreCase);

        var json = JsonDocument.Parse(result);
        Assert.Equal(25, json.RootElement.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task PokeApiTools_GetMove_WorksDirectly()
    {
        // Arrange
        var httpClient = new HttpClient();
        var logger = NullLogger<es.vargontoc.nuzlocke.ai.Services.PokeApiConnector>.Instance;
        var options = Options.Create(new PokeApiOptions());
        var connector = new es.vargontoc.nuzlocke.ai.Services.PokeApiConnector(httpClient, logger, options);

        // Act
        var result = await PokeApiTools.GetMove(connector, "thunderbolt");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("thunderbolt", result, StringComparison.OrdinalIgnoreCase);

        var json = JsonDocument.Parse(result);
        Assert.True(json.RootElement.GetProperty("power").GetInt32() > 0);
    }

    [Fact]
    public async Task PokeApiTools_GetType_WorksDirectly()
    {
        // Arrange
        var httpClient = new HttpClient();
        var logger = NullLogger<es.vargontoc.nuzlocke.ai.Services.PokeApiConnector>.Instance;
        var options = Options.Create(new PokeApiOptions());
        var connector = new es.vargontoc.nuzlocke.ai.Services.PokeApiConnector(httpClient, logger, options);

        // Act
        var result = await PokeApiTools.GetType(connector, "electric");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("electric", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("damage_relations", result);
    }

    [Fact]
    public async Task PokeApiTools_GetAbility_WorksDirectly()
    {
        // Arrange
        var httpClient = new HttpClient();
        var logger = NullLogger<es.vargontoc.nuzlocke.ai.Services.PokeApiConnector>.Instance;
        var options = Options.Create(new PokeApiOptions());
        var connector = new es.vargontoc.nuzlocke.ai.Services.PokeApiConnector(httpClient, logger, options);

        // Act
        var result = await PokeApiTools.GetAbility(connector, "overgrow");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("overgrow", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("effect_entries", result);
    }

    [Fact]
    public async Task PokeApiTools_GetPokemon_InvalidName_ReturnsError()
    {
        // Arrange
        var httpClient = new HttpClient();
        var logger = NullLogger<es.vargontoc.nuzlocke.ai.Services.PokeApiConnector>.Instance;
        var options = Options.Create(new PokeApiOptions());
        var connector = new es.vargontoc.nuzlocke.ai.Services.PokeApiConnector(httpClient, logger, options);

        // Act
        var result = await PokeApiTools.GetPokemon(connector, "invalidpokemon999");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("error", result, StringComparison.OrdinalIgnoreCase);
    }
}
