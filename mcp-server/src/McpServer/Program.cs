using es.vargontoc.nuzlocke.ai.Services;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddHttpClient<IPokeApiConnector, PokeApiConnector>();

var app = builder.Build();

app.MapGet("/", () => "AI Pokemon Nuzlocker MCP Server");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

// Partial Program class to support WebApplicationFactory in integration tests
namespace es.vargontoc.nuzlocke.ai
{
	public partial class Program { }
}
