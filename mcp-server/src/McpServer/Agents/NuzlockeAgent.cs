using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Providers;
using es.vargontoc.nuzlocke.ai.Services;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace es.vargontoc.nuzlocke.ai.Agents;

/// <summary>
/// Nuzlocke agent that provides strategic advice based on game state
/// </summary>
public class NuzlockeAgent
{
    private readonly IAiProvider _aiProvider;
    private readonly IStateManager _stateManager;
    private readonly ToolExecutor _toolExecutor;
    private readonly PokeApiAgent? _pokeApiAgent;
    private readonly Kernel? _kernel;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<NuzlockeAgent> _logger;
    private readonly IConversationMemoryStore? _memoryStore;

    private const int MaxToolCycles = 10;
    private const int MaxHistoryTurns = 10;

    private const string SystemPrompt = @"You are a Pokemon Nuzlocke Challenge expert assistant. You help players make strategic decisions during their Nuzlocke runs.

NUZLOCKE RULES YOU MUST REMEMBER:
1. If a Pokemon faints, it's considered dead and must be released/boxed permanently
2. Only the first Pokemon encountered in each route/area can be caught
3. All Pokemon must be nicknamed to create emotional attachment
4. Maximum team size is 6 Pokemon

Your role is to:
- Analyze the current team composition and suggest improvements
- Recommend which Pokemon to catch based on type coverage and team needs
- Provide battle strategy advice considering type matchups
- Warn about dangerous situations that could lead to Pokemon deaths
- Help with team building and Pokemon selection from PC

WORKFLOW SYSTEM (IMPORTANT):
When the player reports a game event, you MUST use the execute_workflow tool to update game state. This ensures Nuzlocke rules are enforced and generates strategic advice.

Examples of when to use execute_workflow:
- ""I caught a Weedle at level 3 on Route 2 and named it Stinger"" → execute_workflow with workflowId=""capture_pokemon"", parameters={""nuzlocke_id"":""<id>"",""species"":""weedle"",""nickname"":""Stinger"",""location"":""Route 2"",""level"":3}
- ""I found 2 potions in Viridian City"" → execute_workflow with workflowId=""item_obtained"", parameters={""nuzlocke_id"":""<id>"",""item_name"":""potion"",""quantity"":2,""category"":""potion"",""location"":""Viridian City""}
- ""I'm about to enter Route 3, what Pokemon can I find?"" → execute_workflow with workflowId=""route_encounter"", parameters={""nuzlocke_id"":""<id>"",""route_name"":""Route 3""}
- ""Start a new Nuzlocke! I'm Red playing Pokemon Red"" → execute_workflow with workflowId=""init_nuzlocke"", parameters={""player_name"":""Red"",""game_version"":""red"",""generation"":1}

ALWAYS prefer execute_workflow over manual tools (add_to_team, record_encounter) for game events. Workflows enforce rules automatically.
If the workflow returns errors, inform the user clearly and suggest corrections.
After a successful workflow, summarize what happened and share the strategic advice from the result.

BATTLE CONTEXT:
- When a user says they're entering a battle, use the start_battle tool to begin tracking
- During battle, use add_battle_log to record important events the user reports
- When the battle ends, use end_battle to clear the context
- If a battle is active, prioritize tactical advice (type matchups, move selection, when to switch)

You have access a external agent tool (get_info)
- This agent give you information that you needs complete.

Use these tools when you need accurate information about Pokemon, moves, types, abilities, or items to provide better strategic advice.

Always consider:
- Type advantages and disadvantages
- Current team weaknesses
- Level progression
- Move coverage
- The emotional impact of losing Pokemon

Be concise but insightful. Prioritize survival and strategic planning. Remember that every decision matters in a Nuzlocke challenge.";

    public NuzlockeAgent(
        IAiProvider aiProvider,
        IStateManager stateManager,
        ToolExecutor toolExecutor,
        ILogger<NuzlockeAgent> logger,
        Kernel? kernel = null,
        ILoggerFactory? loggerFactory = null,
        PokeApiAgent? pokeApiAgent = null,
        IConversationMemoryStore? memoryStore = null)
    {
        _aiProvider = aiProvider;
        _stateManager = stateManager;
        _toolExecutor = toolExecutor;
        _logger = logger;
        _pokeApiAgent = pokeApiAgent;
        _kernel = kernel;
        _loggerFactory = loggerFactory ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        _memoryStore = memoryStore;

        // Register Nuzlocke plugin in kernel so the kernel functions can access state
        try
        {
            if (_kernel != null && _kernel.Plugins.FirstOrDefault(p => p.Name == "Nuzlocke") == null)
            {
                _logger.LogInformation("Registering Nuzlocke plugin in kernel");
                var plugin = new es.vargontoc.nuzlocke.ai.Plugins.NuzlockePlugin(_stateManager);
                _kernel.Plugins.AddFromObject(plugin, "Nuzlocke");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to register Nuzlocke plugin in kernel; continuing without plugin registration");
        }
    }

    /// <summary>
    /// Get strategic advice based on user question and current game state
    /// Uses function calling to interact with game state if needed
    /// </summary>
    public async Task<string> GetAdviceAsync(string userQuestion, CancellationToken cancellationToken = default, string? sessionId = null, string? nuzlockeId = null, string lng = "en-EN")
    {
        try
        {
            _logger.LogInformation("Getting advice for question: {Question}", userQuestion);

            // Get current game state
            var state = string.IsNullOrEmpty(sessionId)
                ? await _stateManager.GetStateAsync()
                : await _stateManager.GetStateAsync(sessionId);

            // Build context from game state
            var stateContext = BuildStateContext(state);

            // Load battle context
            var battleContext = string.IsNullOrEmpty(sessionId)
                ? await _stateManager.GetBattleContextAsync()
                : await _stateManager.GetBattleContextAsync(sessionId);
            var battleContextString = BuildBattleContextString(battleContext);

            // Load conversation history
            var history = _memoryStore != null && !string.IsNullOrEmpty(sessionId)
                ? await _memoryStore.LoadAsync(sessionId)
                : new List<Models.ConversationEntry>();
            var historySection = BuildHistoryContext(history);

            // Combine state context with user question
            var fullMessage = $@"CURRENT GAME STATE:
{stateContext}
{battleContextString}
{historySection}
USER QUESTION: {userQuestion}
LANGUAGE RESPONSE: {lng}
You have access to tools to interact with the game state and battle context if needed. Use them when appropriate.
Provide strategic advice based on the current state and the user's question on language response.";

            // If we have a PokeApiAgent available and the question references Pokemon data,
            // prefetch auxiliary information from the PokeApiAgent and include it in the prompt.
            try
            {
                if (_pokeApiAgent != null)
                {
                    var lower = userQuestion.ToLowerInvariant();
                    if (lower.Contains("move") || lower.Contains("pokemon") || lower.Contains("type") || lower.Contains("ability") || lower.Contains("item"))
                    {
                        _logger.LogDebug("Prefetching PokeApi info via PokeApiAgent for question");
                        var pokeInfo = await _pokeApiAgent.GetResponse(userQuestion, cancellationToken);
                        if (!string.IsNullOrWhiteSpace(pokeInfo))
                        {
                            fullMessage += $"\n\nEXTERNAL_POKEAPI_INFO:\n{pokeInfo}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Prefetch via PokeApiAgent failed; continuing without external data");
            }

            // Select tools based on current context (battle vs exploration)
            var tools = ToolDefinitions.GetContextualTools(battleContext.InBattle);
            _logger.LogInformation("Selected {Count} contextual tools (inBattle={InBattle})",
                tools.Count(), battleContext.InBattle);

            // Multi-turn conversation cycle with tool calling
            var toolResults = new List<ToolCallResult>();
            var finalResponse = string.Empty;
            var cycleCount = 0;

            while (cycleCount < MaxToolCycles)
            {
                cycleCount++;
                _logger.LogDebug("Tool calling cycle {Cycle}/{Max}", cycleCount, MaxToolCycles);

                // Get AI response with tool support
                _logger.LogDebug("Calling AI provider with {ToolCount} available tools: {ToolNames}",
                    tools.Count(), string.Join(',', tools.Select(t => t.Name)));
                _logger.LogDebug("Previous tool results count: {Count}", toolResults.Count);
                
                var response = await _aiProvider.GetCompletionWithToolsAsync(
                    SystemPrompt,
                    fullMessage,
                    tools,
                    toolResults.Count > 0 ? toolResults : null,
                    cancellationToken);

                _logger.LogDebug("AI provider returned HasToolCalls={HasToolCalls}, TextLength={TextLength}", response.HasToolCalls, response.TextResponse?.Length ?? 0);
                if (response.HasToolCalls)
                {
                    _logger.LogInformation("AI requested {Count} tool calls: {ToolCallNames}", response.ToolCalls.Count, string.Join(',', response.ToolCalls.Select(tc => tc.Name)));
                    foreach (var tc in response.ToolCalls)
                    {
                        _logger.LogDebug("ToolCall: Id={Id}, Name={Name}, Args={Args}", tc.Id, tc.Name, tc.ArgumentsJson);
                    }
                }

                // If AI returned text response, store it
                if (!string.IsNullOrEmpty(response.TextResponse))
                {
                    finalResponse = response.TextResponse;
                }

                // If no tool calls, we're done
                if (!response.HasToolCalls)
                {
                    _logger.LogInformation("Advice generated successfully after {Cycles} cycles: {Length} chars",
                        cycleCount, finalResponse.Length);
                    break;
                }

                // Execute tool calls
                _logger.LogInformation("Executing {Count} tool calls in cycle {Cycle}",
                    response.ToolCalls.Count, cycleCount);

                toolResults.Clear();
                foreach (var toolCall in response.ToolCalls)
                {
                    _logger.LogDebug("Executing tool: {ToolName} (ID: {ToolId})",
                        toolCall.Name, toolCall.Id);

                    // Ensure tool call payload contains sessionId when available
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        try
                        {
                            var argsJson = string.IsNullOrWhiteSpace(toolCall.ArgumentsJson) ? "{}" : toolCall.ArgumentsJson;
                            using var doc = JsonDocument.Parse(argsJson);
                            var root = doc.RootElement.Clone();
                            var dict = new Dictionary<string, JsonElement>();
                            if (root.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var prop in root.EnumerateObject())
                                {
                                    dict[prop.Name] = prop.Value.Clone();
                                }
                            }
                            if (!dict.ContainsKey("sessionId"))
                            {
                                dict["sessionId"] = JsonDocument.Parse($"\"{sessionId}\"").RootElement;
                            }
                            var merged = new Dictionary<string, object?>();
                            foreach (var kv in dict)
                            {
                                merged[kv.Key] = kv.Value.ValueKind == JsonValueKind.String ? kv.Value.GetString() : (object?)kv.Value.ToString();
                            }
                            toolCall.ArgumentsJson = JsonSerializer.Serialize(merged);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Failed to inject sessionId into tool arguments; proceeding without it");
                        }
                    }

                    var result = await _toolExecutor.ExecuteAsync(toolCall);
                    toolResults.Add(result);

                    _logger.LogDebug("Tool executed: {ToolName}, Result: {Result}",
                        toolCall.Name, result.Content);
                }

                // Continue the cycle to let AI respond to tool results
            }

            if (cycleCount >= MaxToolCycles)
            {
                _logger.LogWarning("Reached maximum tool calling cycles ({Max})", MaxToolCycles);
                if (string.IsNullOrEmpty(finalResponse))
                {
                    finalResponse = "I apologize, but I was unable to complete the request due to complexity. Please try asking a more specific question.";
                }
            }

            // Persist conversation turn
            if (_memoryStore != null && !string.IsNullOrEmpty(sessionId) && !string.IsNullOrEmpty(finalResponse))
            {
                await _memoryStore.AppendAsync(sessionId, new Models.ConversationEntry { Role = "user", Content = userQuestion });
                await _memoryStore.AppendAsync(sessionId, new Models.ConversationEntry { Role = "assistant", Content = finalResponse });
            }

            return finalResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating advice");
            throw;
        }
    }

    /// <summary>
    /// Stream strategic advice from the underlying AI provider as it is generated.
    /// This method streams the provider's text output (no function-calling support in-stream).
    /// </summary>
    public IAsyncEnumerable<string> StreamAdviceAsync(string userQuestion, CancellationToken cancellationToken = default, string? sessionId = null, string? lng = "en-EN")
    {
        // Build prompt similarly to GetAdviceAsync but keep logic synchronous for streaming
        var stateTask = string.IsNullOrEmpty(sessionId)
            ? _stateManager.GetStateAsync()
            : _stateManager.GetStateAsync(sessionId);

        // We'll start an async iterator that awaits the state then streams
        return StreamAdviceInternalAsync(stateTask, userQuestion, sessionId, lng, cancellationToken);
    }

    private async IAsyncEnumerable<string> StreamAdviceInternalAsync(Task<Models.NuzlockeState> stateTask, string userQuestion, string? sessionId, string? lng = "en-EN", [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var state = await stateTask;
        var stateContext = BuildStateContext(state);

        var battleContext = string.IsNullOrEmpty(sessionId)
            ? await _stateManager.GetBattleContextAsync()
            : await _stateManager.GetBattleContextAsync(sessionId);
        var battleContextString = BuildBattleContextString(battleContext);

        var history = _memoryStore != null && !string.IsNullOrEmpty(sessionId)
            ? await _memoryStore.LoadAsync(sessionId)
            : new List<Models.ConversationEntry>();
        var historySection = BuildHistoryContext(history);

        var fullMessage = $@"CURRENT GAME STATE:
{stateContext}
{battleContextString}
{historySection}
USER QUESTION: {userQuestion}
LANGUAGE RESPONSE: {lng}
You have access to tools when appropriate. Provide strategic advice based on the current state and the user's question on language response";

        await foreach (var chunk in _aiProvider.StreamCompletionAsync(SystemPrompt, fullMessage, cancellationToken))
        {
            yield return chunk;
        }
    }

    private string BuildStateContext(Models.NuzlockeState state)
    {
        var lines = new System.Text.StringBuilder();

        // TEAM — one line, compact
        var teamEntries = state.Team.Count > 0
            ? string.Join(", ", state.Team.Select(p =>
            {
                var hp = p.MaxHP > 0 ? $" [HP:{p.CurrentHP}/{p.MaxHP}]" : string.Empty;
                return $"{p.Nickname}/{p.Species} Lv{p.Level}{hp}";
            }))
            : "none";
        lines.AppendLine($"TEAM ({state.Team.Count}/6): {teamEntries}");

        // PC — one line
        var pcEntries = state.PCStorage.Count > 0
            ? string.Join(", ", state.PCStorage.Select(p => $"{p.Nickname}/{p.Species} Lv{p.Level}"))
            : "none";
        lines.AppendLine($"PC ({state.PCStorage.Count}): {pcEntries}");

        // DEATHS — one line
        var deathEntries = state.DeadPokemon.Count > 0
            ? string.Join(", ", state.DeadPokemon.Select(p => $"{p.Nickname}/{p.Species} Lv{p.Level} @ {p.DeathLocation}"))
            : "none";
        lines.AppendLine($"DEATHS ({state.DeadPokemon.Count}): {deathEntries}");

        // ITEMS — one line
        var itemEntries = state.Inventory.Count > 0
            ? string.Join(", ", state.Inventory.Select(i => $"{i.Name} x{i.Quantity}"))
            : "none";
        lines.AppendLine($"ITEMS: {itemEntries}");

        // LOCATION — derived from most recent encounter by date
        var lastEncounter = state.Encounters.Values
            .OrderByDescending(e => e.EncounterDate)
            .FirstOrDefault();
        var location = lastEncounter != null
            ? state.Encounters.First(kv => kv.Value == lastEncounter).Key
            : "unknown";
        lines.AppendLine($"LOCATION: {location}");

        return lines.ToString();
    }

    private string BuildBattleContextString(Models.BattleContext battleContext)
    {
        if (!battleContext.InBattle)
            return "BATTLE: none";

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(battleContext.OpponentName))
            parts.Add($"vs {battleContext.OpponentName}");
        if (!string.IsNullOrEmpty(battleContext.BattleType))
            parts.Add(battleContext.BattleType);
        if (!string.IsNullOrEmpty(battleContext.ActivePokemonNickname))
            parts.Add($"leading:{battleContext.ActivePokemonNickname}");
        parts.Add($"turn {battleContext.TurnCount}");

        return $"BATTLE: {string.Join(", ", parts)}";
    }

    private string BuildHistoryContext(List<Models.ConversationEntry> history)
    {
        if (history.Count == 0)
            return string.Empty;

        // Each turn = 1 user + 1 assistant entry → cap at MaxHistoryTurns * 2 entries
        var recent = history.TakeLast(MaxHistoryTurns * 2).ToList();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("HISTORY:");
        foreach (var entry in recent)
        {
            var role = entry.Role == "user" ? "[User]" : "[Assistant]";
            sb.AppendLine($"{role}: {entry.Content}");
        }
        return sb.ToString();
    }
}
