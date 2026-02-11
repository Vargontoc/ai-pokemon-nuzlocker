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
    private readonly Kernel _kernel;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<NuzlockeAgent> _logger;

    private const int MaxToolCycles = 10;

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
        Kernel kernel,
        ILoggerFactory loggerFactory,
        PokeApiAgent? pokeApiAgent = null)
    {
        _aiProvider = aiProvider;
        _stateManager = stateManager;
        _toolExecutor = toolExecutor;
        _logger = logger;
        _pokeApiAgent = pokeApiAgent;
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

        // Register Nuzlocke plugin in kernel so the kernel functions can access state
        try
        {
            if (_kernel.Plugins.FirstOrDefault(p => p.Name == "Nuzlocke") == null)
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
    public async Task<string> GetAdviceAsync(string userQuestion, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting advice for question: {Question}", userQuestion);

            // Get current game state
            var state = await _stateManager.GetStateAsync();

            // Build context from game state
            var stateContext = BuildStateContext(state);

            // Combine state context with user question
            var fullMessage = $@"CURRENT GAME STATE:
{stateContext}

USER QUESTION: {userQuestion}

You have access to tools to interact with the game state if needed. Use them when appropriate.
Provide strategic advice based on the current state and the user's question.";

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

            // Get available tools
            // TEMPORARY: Only using Nuzlocke tools to debug Ollama crash with 10 tools
            var tools = ToolDefinitions.AllTools;

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

            return finalResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating advice");
            throw;
        }
    }

    private string BuildStateContext(Models.NuzlockeState state)
    {
        var context = new System.Text.StringBuilder();

        // Team information
        context.AppendLine($"TEAM ({state.Team.Count}/6):");
        if (state.Team.Count == 0)
        {
            context.AppendLine("  - No Pokemon in team yet");
        }
        else
        {
            foreach (var pokemon in state.Team)
            {
                var hpInfo = pokemon.MaxHP > 0
                    ? $"HP: {pokemon.CurrentHP}/{pokemon.MaxHP}"
                    : "HP: Unknown";
                var moves = pokemon.Moves.Count > 0
                    ? string.Join(", ", pokemon.Moves)
                    : "no moves recorded";
                context.AppendLine($"  - {pokemon.Nickname} ({pokemon.Species}, Lv.{pokemon.Level}) - {hpInfo} - Moves: {moves}");
            }
        }

        // PC Storage
        context.AppendLine($"\nPC STORAGE ({state.PCStorage.Count}):");
        if (state.PCStorage.Count == 0)
        {
            context.AppendLine("  - Empty");
        }
        else
        {
            foreach (var pokemon in state.PCStorage)
            {
                context.AppendLine($"  - {pokemon.Nickname} ({pokemon.Species}, Lv.{pokemon.Level})");
            }
        }

        // Deaths (Graveyard)
        context.AppendLine($"\nDEATHS ({state.DeadPokemon.Count}):");
        if (state.DeadPokemon.Count == 0)
        {
            context.AppendLine("  - No casualties yet");
        }
        else
        {
            foreach (var pokemon in state.DeadPokemon)
            {
                context.AppendLine($"  - {pokemon.Nickname} ({pokemon.Species}, Lv.{pokemon.Level}) - Died at {pokemon.DeathLocation}: {pokemon.CauseOfDeath}");
            }
        }

        // Encounters
        context.AppendLine($"\nENCOUNTERS USED ({state.Encounters.Count} locations):");
        if (state.Encounters.Count == 0)
        {
            context.AppendLine("  - No encounters recorded yet");
        }
        else
        {
            foreach (var encounter in state.Encounters)
            {
                var captured = encounter.Value.EncounterUsed
                    ? $"Caught: {encounter.Value.CapturedNickname} ({encounter.Value.CapturedSpecies})"
                    : "Failed/Skipped";
                context.AppendLine($"  - {encounter.Key}: {captured}");
            }
        }

        return context.ToString();
    }
}
