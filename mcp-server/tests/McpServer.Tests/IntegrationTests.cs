using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace es.vargontoc.nuzlocke.ai.Tests;

[Collection("WebApp collection")]
public class IntegrationTests
{
    private readonly WebApplicationFactory<es.vargontoc.nuzlocke.ai.Program> _factory;

    public IntegrationTests(WebApplicationFactory<es.vargontoc.nuzlocke.ai.Program> factory) => _factory = factory;

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        var res = await client.GetAsync("/health");
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("ok", doc.RootElement.GetProperty("status").GetString());
    }
}
