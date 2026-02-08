using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests;

[Collection("WebApp collection")]
public class HealthCheckTests
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HealthCheckTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonDocument.Parse(content);

        var status = healthReport.RootElement.GetProperty("status").GetString();
        Assert.Equal("Healthy", status);
    }

    [Fact]
    public async Task HealthEndpoint_IncludesChecksDetails()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonDocument.Parse(content);

        Assert.True(healthReport.RootElement.TryGetProperty("checks", out var checks));
        Assert.True(checks.GetArrayLength() > 0);

        // Should have database and pokeapi checks
        var checksArray = checks.EnumerateArray();
        var checkNames = new List<string>();
        foreach (var check in checksArray)
        {
            checkNames.Add(check.GetProperty("name").GetString()!);
        }

        Assert.Contains("database", checkNames);
        Assert.Contains("pokeapi", checkNames);
    }

    [Fact]
    public async Task HealthReadyEndpoint_ChecksDatabase()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonDocument.Parse(content);

        var checks = healthReport.RootElement.GetProperty("checks").EnumerateArray();
        var databaseCheck = checks.FirstOrDefault(c =>
            c.GetProperty("name").GetString() == "database");

        Assert.True(databaseCheck.ValueKind != JsonValueKind.Undefined);
        Assert.Equal("Healthy", databaseCheck.GetProperty("status").GetString());
    }

    [Fact]
    public async Task HealthReadyEndpoint_ChecksPokeApi()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        // May be degraded if PokeApi is slow/unavailable, but should respond
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.ServiceUnavailable);

        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonDocument.Parse(content);

        var checks = healthReport.RootElement.GetProperty("checks").EnumerateArray();
        var pokeApiCheck = checks.FirstOrDefault(c =>
            c.GetProperty("name").GetString() == "pokeapi");

        Assert.True(pokeApiCheck.ValueKind != JsonValueKind.Undefined);
        var status = pokeApiCheck.GetProperty("status").GetString();
        Assert.True(status == "Healthy" || status == "Degraded" || status == "Unhealthy");
    }

    [Fact]
    public async Task HealthLiveEndpoint_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonDocument.Parse(content);

        var status = healthReport.RootElement.GetProperty("status").GetString();
        Assert.Equal("Healthy", status);

        // Live endpoint should have no checks (just confirms app is running)
        var checks = healthReport.RootElement.GetProperty("checks");
        Assert.Equal(0, checks.GetArrayLength());
    }

    [Fact]
    public async Task HealthEndpoint_IncludesTotalDuration()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonDocument.Parse(content);

        Assert.True(healthReport.RootElement.TryGetProperty("totalDuration", out var duration));
        Assert.True(duration.GetDouble() >= 0);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsJsonContentType()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }
}
