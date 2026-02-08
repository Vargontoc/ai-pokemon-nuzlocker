using es.vargontoc.nuzlocke.ai.Configuration;
using es.vargontoc.nuzlocke.ai.Data;
using es.vargontoc.nuzlocke.ai.Repositories;
using es.vargontoc.nuzlocke.ai.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ModelContextProtocol.Server;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure options from appsettings
builder.Services.Configure<PokeApiOptions>(
    builder.Configuration.GetSection(PokeApiOptions.SectionName));

// Register SQLite database
var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? "Data Source=pokecache.db";
builder.Services.AddDbContext<PokeDbContext>(options =>
    options.UseSqlite(connectionString));

// Register cache repositories
builder.Services.AddScoped<ICacheRepository<CachedPokemon>, PokemonCacheRepository>();
builder.Services.AddScoped<ICacheRepository<CachedMove>, MoveCacheRepository>();
builder.Services.AddScoped<ICacheRepository<CachedType>, TypeCacheRepository>();
builder.Services.AddScoped<ICacheRepository<CachedAbility>, AbilityCacheRepository>();

// Register PokeApi connector (direct)
builder.Services.AddHttpClient<PokeApiConnector>();

// Register cached connector as IPokeApiConnector
builder.Services.AddScoped<IPokeApiConnector, CachedPokeApiConnector>();

// Register Nuzlocke state manager
builder.Services.AddSingleton<IStateManager, StateManager>();

// Configure MCP Server
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

// Configure Health Checks
var pokeApiBaseUrl = builder.Configuration.GetSection(PokeApiOptions.SectionName)
    .Get<PokeApiOptions>()?.BaseUrl ?? "https://pokeapi.co/api/v2";

builder.Services.AddHealthChecks()
    .AddDbContextCheck<PokeDbContext>(
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready", "db" })
    .AddUrlGroup(
        new Uri(pokeApiBaseUrl),
        name: "pokeapi",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "ready", "external" },
        timeout: TimeSpan.FromSeconds(3));

var app = builder.Build();

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PokeDbContext>();
    db.Database.Migrate();
}


app.MapGet("/", () => "AI Pokemon Nuzlocker MCP Server");

// Health check endpoints
var healthCheckOptions = new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        }, new JsonSerializerOptions { WriteIndented = true });
        await context.Response.WriteAsync(result);
    }
};

app.MapHealthChecks("/health", healthCheckOptions);
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = healthCheckOptions.ResponseWriter
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false, // No checks, just confirms app is running
    ResponseWriter = healthCheckOptions.ResponseWriter
});

// Map MCP endpoints
app.MapMcp("/mcp");

app.Run();

// Partial Program class to support WebApplicationFactory in integration tests
namespace es.vargontoc.nuzlocke.ai
{
	public partial class Program { }
}
