using es.vargontoc.nuzlocke.ai.Services;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddHttpClient<IPokeApiConnector, PokeApiConnector>();

// Configure MCP Server
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

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
