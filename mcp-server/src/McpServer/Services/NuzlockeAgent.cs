using System.Text.Json;

namespace es.vargontoc.nuzlocke.ai.Services;

/// <summary>
/// Nuzlocke agent that provides strategic advice based on game state
/// </summary>
public class NuzlockeAgent
{
    private readonly IAiProvider _aiProvider;
    private readonly IStateManager _stateManager;
    private readonly ILogger<NuzlockeAgent> _logger;

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
        ILogger<NuzlockeAgent> logger)
    {
        _aiProvider = aiProvider;
        _stateManager = stateManager;
        _logger = logger;
    }

    /// <summary>
    /// Get strategic advice based on user question and current game state
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

Provide strategic advice based on the current state and the user's question.";

            // Get AI response
            var advice = await _aiProvider.GetCompletionAsync(
                SystemPrompt,
                fullMessage,
                cancellationToken);

            _logger.LogInformation("Advice generated successfully: {Length} chars", advice.Length);
            return advice;
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
