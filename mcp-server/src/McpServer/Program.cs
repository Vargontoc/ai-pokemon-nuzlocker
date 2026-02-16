using es.vargontoc.nuzlocke.ai.Agents;
using es.vargontoc.nuzlocke.ai.Configuration;
using es.vargontoc.nuzlocke.ai.Connectors;
using es.vargontoc.nuzlocke.ai.Connectors.Impl;
using es.vargontoc.nuzlocke.ai.Data;
using es.vargontoc.nuzlocke.ai.Repositories;
using es.vargontoc.nuzlocke.ai.Services;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Providers.Impl;
using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.WebSockets;
using es.vargontoc.nuzlocke.ai.Workflows;
using es.vargontoc.nuzlocke.ai.Workflows.Setup;
using es.vargontoc.nuzlocke.ai.Workflows.Gameplay;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

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
    // If provider not configured, default to Ollama if base url present, otherwise throw
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

// Configure Semantic Kernel with provider-specific setup (without plugins - they're scoped)
// Register Kernel as scoped so plugins added at runtime can safely reference scoped services
builder.Services.AddScoped<Kernel>(sp =>
{
    var options = sp.GetRequiredService<IOptions<AiOptions>>().Value;
    var logger = sp.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("=== Semantic Kernel Configuration ===");
    logger.LogInformation("Provider: {Provider}", options.Provider);
    logger.LogInformation("Model: {Model}", options.Model);
    logger.LogInformation("======================================");

    var kernelBuilder = Kernel.CreateBuilder();

    // Add the appropriate chat completion service based on provider
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
            // NOTE: Semantic Kernel doesn't have native Claude support yet
            // You would need to use Anthropic SDK directly or wait for SK support
            throw new NotImplementedException(
                "Claude provider with Semantic Kernel is not yet implemented. " +
                "Use OpenAI or Ollama for now.");

        default:
            throw new InvalidOperationException(
                $"Unknown AI provider: {options.Provider}. " +
                $"Valid options are: Ollama, OpenAI");
    }

    // Don't add plugins here - they need scoped services
    // Plugins will be added in NuzlockeKernelAgent constructor

    return kernelBuilder.Build();
});

// Register Semantic Kernel agent (scoped to properly handle plugins)
builder.Services.AddScoped<PokeApiAgent>();
// Register NuzlockeAgent so it can be injected into minimal API endpoints
builder.Services.AddScoped<NuzlockeAgent>();

// Register WebSocket services (singletons — manage cross-request connections)
builder.Services.AddSingleton<IAdviceConnectionManager, AdviceConnectionManager>();
builder.Services.AddSingleton<IAdviceDispatcher, AdviceBackgroundDispatcher>();

// Register Workflow System
builder.Services.AddScoped<IWorkflowEngine, WorkflowEngine>();
builder.Services.AddScoped<IWorkflow, InitNuzlockeWorkflow>();
builder.Services.AddScoped<IWorkflow, CapturePokemonWorkflow>();
// Future workflows:
// builder.Services.AddScoped<IWorkflow, StartBattleWorkflow>();
// etc.

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

// Global logging middleware to capture unhandled exceptions and response codes
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

// AI Agent endpoint (powered by Semantic Kernel)
app.MapPost("/agent/advice", async (AdviceRequest request, PokeApiAgent agent, CancellationToken ct) =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("POST /agent/advice received: {Question}", request.Question);
    try
    {
        var advice = await agent.GetResponse(request.Question);
        return Results.Ok(new { question = request.Question, advice });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error handling /agent/advice for question: {Question}", request.Question);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.MapPost("/nuzlocke/advice", async (AdviceRequest request, NuzlockeAgent agent, CancellationToken ct) =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("POST /nuzlocke/advice received: {Question}, session={SessionId}", request.Question, request.SessionId);
    try
    {
        var advice = await agent.GetAdviceAsync(request.Question, ct, request.SessionId);
        return Results.Ok(new { question = request.Question, advice });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error handling /nuzlocke/advice for question: {Question}", request.Question);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// Streaming endpoint for nuzlocke advice (SSE)
app.MapPost("/nuzlocke/advice/stream", async (AdviceRequest request, NuzlockeAgent agent, HttpContext ctx, CancellationToken ct) =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("POST /nuzlocke/advice/stream received: {Question}, session={SessionId}", request.Question, request.SessionId);

    ctx.Response.Headers["Cache-Control"] = "no-cache";
    ctx.Response.ContentType = "text/event-stream";

    try
    {
        await foreach (var chunk in agent.StreamAdviceAsync(request.Question, ct, request.SessionId))
        {
            if (ct.IsCancellationRequested) break;
            // Write SSE data field
            await ctx.Response.WriteAsync($"data: {chunk.Replace("\n", "\\n")}\n\n");
            await ctx.Response.Body.FlushAsync(ct);
        }

        // Close the stream
        await ctx.Response.WriteAsync("event: end\ndata: [DONE]\n\n");
        await ctx.Response.Body.FlushAsync(ct);
        return Results.Ok();
    }
    catch (OperationCanceledException)
    {
        return Results.Problem(detail: "Client cancelled stream", statusCode: 499);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error while streaming advice");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// WebSocket endpoint for advice streaming
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
        // Keep connection alive — read loop waits for client close
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

// Workflow endpoints
app.MapPost("/nuzlocke/workflow", async (WorkflowRequest request, IWorkflowEngine engine, CancellationToken ct) =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("POST /nuzlocke/workflow: {WorkflowId}, session={SessionId}",
        request.WorkflowId, request.SessionId);
    try
    {
        var result = await engine.ExecuteWithAsyncAdviceAsync(request, ct);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error executing workflow {WorkflowId}", request.WorkflowId);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.MapGet("/nuzlocke/workflows", (IWorkflowEngine engine) =>
    Results.Ok(new { workflows = engine.GetAvailableWorkflows() }));

// Nuzlocke session management endpoints
app.MapPost("/nuzlocke/sessions", async (CreateSessionRequest req, INuzlockeSessionManager sessions) =>
{
    try
    {
        var info = await sessions.CreateSessionAsync(req.Name, req.DirectoryPath);
        return Results.Created($"/nuzlocke/sessions/{info.Id}", info);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 400);
    }
});

app.MapGet("/nuzlocke/sessions", async (INuzlockeSessionManager sessions) =>
    Results.Ok(await sessions.ListSessionsAsync()));

app.MapGet("/nuzlocke/sessions/{id}", async (string id, INuzlockeSessionManager sessions) =>
{
    var s = await sessions.GetSessionAsync(id);
    return s == null ? Results.NotFound() : Results.Ok(s);
});

app.MapDelete("/nuzlocke/sessions/{id}", async (string id, INuzlockeSessionManager sessions) =>
{
    var ok = await sessions.DeleteSessionAsync(id);
    return ok ? Results.NoContent() : Results.NotFound();
});

app.MapGet("/nuzlocke/sessions/{id}/data", async (string id, INuzlockeSessionManager sessions) =>
{
    try
    {
        var data = await sessions.LoadSessionDataAsync(id);
        return Results.Ok(data);
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

app.MapPost("/nuzlocke/sessions/{id}/data", async (string id, NuzlockeFileData fileData, INuzlockeSessionManager sessions) =>
{
    try
    {
        await sessions.SaveSessionDataAsync(id, fileData);
        return Results.NoContent();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

app.Run();

// Request DTOs
record AdviceRequest(string Question, string? SessionId = null);
record CreateSessionRequest(string Name, string DirectoryPath);

// Partial Program class to support WebApplicationFactory in integration tests
namespace es.vargontoc.nuzlocke.ai
{
	public partial class Program { }
}
