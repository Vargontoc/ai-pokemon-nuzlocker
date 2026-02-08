using es.vargontoc.nuzlocke.ai.Configuration;
using es.vargontoc.nuzlocke.ai.Data;
using es.vargontoc.nuzlocke.ai.Repositories;
using es.vargontoc.nuzlocke.ai.Services;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

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

// Configure MCP Server
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PokeDbContext>();
    db.Database.Migrate();
}


app.MapGet("/", () => "AI Pokemon Nuzlocker MCP Server");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Map MCP endpoints
app.MapMcp("/mcp");

app.Run();

// Partial Program class to support WebApplicationFactory in integration tests
namespace es.vargontoc.nuzlocke.ai
{
	public partial class Program { }
}
