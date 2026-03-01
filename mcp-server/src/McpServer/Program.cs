using es.vargontoc.nuzlocke.ai.Agents;
using es.vargontoc.nuzlocke.ai.Configuration;
using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Connectors.Impl;
using es.vargontoc.nuzlocke.ai.Data;
using es.vargontoc.nuzlocke.ai.Repositories;
using es.vargontoc.nuzlocke.ai.Services;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Providers.Impl;
using es.vargontoc.nuzlocke.ai.WebSockets;
using es.vargontoc.nuzlocke.ai.Workflows;
using es.vargontoc.nuzlocke.ai.Workflows.Setup;
using es.vargontoc.nuzlocke.ai.Workflows.Gameplay;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AI Pokemon Nuzlocker API",
        Version = "v1",
        Description = """
            REST API for the AI Pokemon Nuzlocke tracker.

            ## Workflow System
            The core of the API is the workflow engine (`POST /nuzlocke/workflow`).
            Each workflow mutates the game state and optionally generates LLM advice.

            **Available workflow IDs:**
            - `init_nuzlocke` — Start a new Nuzlocke run
            - `capture_pokemon` — Record a capture
            - `route_encounter` — Look up encounter data for a route
            - `item_obtained` — Add items to inventory
            - `level_up` — Level up a Pokemon (recalculates Gen 1 stats)
            - `manage_moves` — Register current moves (Vertiente A) or learn a new move with LLM analysis (Vertiente B)
            - `evolution` — Evolve a Pokemon with LLM analysis of new capabilities
            - `set_personality` — Change the agent's response personality

            ## Real-time Advice
            Use `POST /nuzlocke/advice/stream` for Server-Sent Events (SSE) streaming,
            or `POST /nuzlocke/advice` + WebSocket at `ws://host/ws/advice?sessionId=<id>` for async dispatch.
            """
    });

    // Include XML comments from this assembly
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);

    // Remove [FromServices] DI params from operation descriptions (they're not API params)
    options.OperationFilter<es.vargontoc.nuzlocke.ai.Configuration.FromServicesOperationFilter>();

    // Add WebSocket and health check endpoints (not controller-based)
    options.DocumentFilter<es.vargontoc.nuzlocke.ai.Configuration.ExtraEndpointsDocumentFilter>();
});

// Configure CORS — restrict to web app origin
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure options from appsettings
builder.Services.Configure<PokeApiOptions>(
    builder.Configuration.GetSection(PokeApiOptions.SectionName));

// Register in-memory cache (L1 for PokeAPI data)
builder.Services.AddMemoryCache();

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
builder.Services.AddScoped<ICacheRepository<CachedItem>, ItemCacheRepository>();

// Register PokeApi connector (direct)
builder.Services.AddHttpClient<PokeApiConnector>();

// Register cached connector as IPokeApiConnector (factory to inject concrete PokeApiConnector as inner)
builder.Services.AddScoped<IPokeApiConnector>(sp =>
    new CachedPokeApiConnector(
        sp.GetRequiredService<PokeApiConnector>(),
        sp.GetRequiredService<ICacheRepository<CachedPokemon>>(),
        sp.GetRequiredService<ICacheRepository<CachedMove>>(),
        sp.GetRequiredService<ICacheRepository<CachedType>>(),
        sp.GetRequiredService<ICacheRepository<CachedAbility>>(),
        sp.GetRequiredService<ICacheRepository<CachedItem>>(),
        sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
        sp.GetRequiredService<ILogger<CachedPokeApiConnector>>(),
        sp.GetRequiredService<IOptions<PokeApiOptions>>()));

// Register Nuzlocke session manager (before StateManager, which depends on it)
builder.Services.AddSingleton<INuzlockeSessionManager, NuzlockeSessionManager>();

// Register Nuzlocke registry repository
builder.Services.AddScoped<INuzlockeRegistryRepository, NuzlockeRegistryRepository>();

// Register Nuzlocke file manager (per-nuzlocke folder structure)
builder.Services.AddScoped<INuzlockeFileManager, NuzlockeFileManager>();

// Register Nuzlocke state manager
builder.Services.AddScoped<IStateManager, StateManager>();

// Register Conversation Memory store
builder.Services.AddScoped<IConversationMemoryStore, FileConversationMemoryStore>();

// Register Stats Calculator (Gen 1)
builder.Services.AddScoped<IStatsCalculator, Gen1StatsCalculator>();

// Register Personality Prompt Provider
builder.Services.AddSingleton<IPersonalityPromptProvider, PersonalityPromptProvider>();

// Register ToolExecutor
builder.Services.AddScoped<ToolExecutor>();

// Configure AI Provider options
builder.Services.Configure<AiOptions>(
    builder.Configuration.GetSection(AiOptions.SectionName));

// Register IAiProvider implementation based on configuration
var aiOptionsSection = builder.Configuration.GetSection(AiOptions.SectionName);
var aiProviderName = aiOptionsSection.GetValue<string>("Provider")?.ToLowerInvariant();
if (aiProviderName == "ollama")
{
    builder.Services.AddHttpClient<OllamaAiProvider>();
    builder.Services.AddScoped<IAiProvider, OllamaAiProvider>();
}
else if (aiProviderName == "openai")
{
    builder.Services.AddScoped<IAiProvider, OpenAiProvider>();
}
else if (aiProviderName == "claude")
{
    builder.Services.AddScoped<IAiProvider, ClaudeAiProvider>();
}
else
{
    var baseUrl = aiOptionsSection.GetValue<string>("BaseUrl");
    if (!string.IsNullOrEmpty(baseUrl))
    {
        builder.Services.AddHttpClient<OllamaAiProvider>();
        builder.Services.AddScoped<IAiProvider, OllamaAiProvider>();
    }
    else
    {
        throw new InvalidOperationException("AI provider not configured. Set AiOptions:Provider to 'Ollama' or 'OpenAI' in appsettings.json");
    }
}

// Configure Semantic Kernel with provider-specific setup
builder.Services.AddScoped<Kernel>(sp =>
{
    var options = sp.GetRequiredService<IOptions<AiOptions>>().Value;
    var logger = sp.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("=== Semantic Kernel Configuration ===");
    logger.LogInformation("Provider: {Provider}", options.Provider);
    logger.LogInformation("Model: {Model}", options.Model);
    logger.LogInformation("======================================");

    var kernelBuilder = Kernel.CreateBuilder();

    switch (options.Provider.ToLower())
    {
        case "ollama":
            kernelBuilder.AddOllamaChatCompletion(
                modelId: options.Model,
                endpoint: new Uri(options.BaseUrl));
            logger.LogInformation("Using Ollama connector at {BaseUrl}", options.BaseUrl);
            break;

        case "openai":
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: options.Model,
                apiKey: options.ApiKey!);
            logger.LogInformation("Using OpenAI connector with model {Model}", options.Model);
            break;

        case "claude":
            throw new NotImplementedException(
                "Claude provider with Semantic Kernel is not yet implemented. " +
                "Use OpenAI or Ollama for now.");

        default:
            throw new InvalidOperationException(
                $"Unknown AI provider: {options.Provider}. " +
                $"Valid options are: Ollama, OpenAI");
    }

    return kernelBuilder.Build();
});

// Register agents
builder.Services.AddScoped<PokeApiAgent>();
builder.Services.AddScoped<NuzlockeAgent>();

// Register WebSocket services (singletons — manage cross-request connections)
builder.Services.AddSingleton<IAdviceConnectionManager, AdviceConnectionManager>();
builder.Services.AddSingleton<IAdviceDispatcher, AdviceBackgroundDispatcher>();

// Register Workflow System
builder.Services.AddScoped<IWorkflowEngine, WorkflowEngine>();
builder.Services.AddScoped<IWorkflow, InitNuzlockeWorkflow>();
builder.Services.AddScoped<IWorkflow, CapturePokemonWorkflow>();
builder.Services.AddScoped<IWorkflow, RouteEncounterWorkflow>();
builder.Services.AddScoped<IWorkflow, ItemObtainedWorkflow>();
builder.Services.AddScoped<IWorkflow, LevelUpWorkflow>();
builder.Services.AddScoped<IWorkflow, SetPersonalityWorkflow>();
builder.Services.AddScoped<IWorkflow, ManageMovesWorkflow>();
builder.Services.AddScoped<IWorkflow, EvolutionWorkflow>();

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

// Global logging middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetService<ILogger<Program>>();
    try
    {
        await next();

        if (context.Response.StatusCode >= 400)
        {
            logger?.LogWarning("Response {StatusCode} for {Method} {Path}", context.Response.StatusCode, context.Request.Method, context.Request.Path);
        }
    }
    catch (Exception ex)
    {
        logger?.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
        throw;
    }
});

// Swagger UI (available in all environments for front-end team access)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "AI Pokemon Nuzlocker API v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "Nuzlocker API Docs";
});

// Enable CORS (before routing)
app.UseCors();

// Enable WebSocket support
app.UseWebSockets();

// Apply migrations and initialize nuzlocke registry
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PokeDbContext>();
    db.Database.Migrate();

    var fileManager = scope.ServiceProvider.GetRequiredService<INuzlockeFileManager>();
    await fileManager.InitializeAsync();
}

// Map controllers (Agent, Nuzlocke, Workflow, Session, Health)
app.MapControllers();

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
    Predicate = _ => false,
    ResponseWriter = healthCheckOptions.ResponseWriter
});

// Map MCP endpoints
app.MapMcp("/mcp");

// WebSocket endpoint for advice streaming (kept as minimal API — WebSocket lifecycle doesn't fit controllers)
app.Map("/ws/advice", async (HttpContext ctx, IAdviceConnectionManager connectionManager) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = 400;
        await ctx.Response.WriteAsync("WebSocket connection required");
        return;
    }

    var sessionId = ctx.Request.Query["sessionId"].ToString();
    if (string.IsNullOrWhiteSpace(sessionId))
    {
        ctx.Response.StatusCode = 400;
        await ctx.Response.WriteAsync("Missing required query parameter: sessionId");
        return;
    }

    var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("WebSocket connection accepted for session {SessionId}", sessionId);

    var webSocket = await ctx.WebSockets.AcceptWebSocketAsync();
    connectionManager.AddConnection(sessionId, webSocket);

    try
    {
        var buffer = new byte[1024];
        while (webSocket.State == System.Net.WebSockets.WebSocketState.Open)
        {
            var receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);

            if (receiveResult.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                break;
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "WebSocket error for session {SessionId}", sessionId);
    }
    finally
    {
        connectionManager.RemoveConnection(sessionId);
        logger.LogInformation("WebSocket disconnected for session {SessionId}", sessionId);
    }
});

app.Run();

// Partial Program class to support WebApplicationFactory in integration tests
namespace es.vargontoc.nuzlocke.ai
{
	public partial class Program { }
}
