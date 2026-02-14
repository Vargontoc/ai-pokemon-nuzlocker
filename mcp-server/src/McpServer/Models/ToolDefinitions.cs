namespace es.vargontoc.nuzlocke.ai.Models;

/// <summary>
/// Definitions of available tools for function calling
/// </summary>
public static class ToolDefinitions
{
    public static readonly ToolDefinition GetGameState = new()
    {
        Name = "get_game_state",
        Description = "Get the complete current state of the Nuzlocke run, including team, dead Pokemon, PC storage, and encounters",
        Parameters = new ToolParameters
        {
            Type = "object",
            Properties = new Dictionary<string, ToolProperty>(),
            Required = new List<string>()
        }
    };

    public static readonly ToolDefinition AddToTeam = new()
    {
        Name = "add_to_team",
        Description = "Add a Pokemon to the current team (maximum 6 Pokemon allowed). Use this when the user catches a new Pokemon.",
        Parameters = new ToolParameters
        {
            Type = "object",
            Properties = new Dictionary<string, ToolProperty>
            {
                ["nickname"] = new ToolProperty
                {
                    Type = "string",
                    Description = "Nickname for the Pokemon (required for Nuzlocke)"
                },
                ["species"] = new ToolProperty
                {
                    Type = "string",
                    Description = "Species name in lowercase (e.g., 'pikachu', 'charizard')"
                },
                ["level"] = new ToolProperty
                {
                    Type = "integer",
                    Description = "Current level of the Pokemon"
                },
                ["caughtAt"] = new ToolProperty
                {
                    Type = "string",
                    Description = "Location where the Pokemon was caught"
                },
                ["currentHP"] = new ToolProperty
                {
                    Type = "integer",
                    Description = "Current HP (optional, default 0)"
                },
                ["maxHP"] = new ToolProperty
                {
                    Type = "integer",
                    Description = "Maximum HP (optional, default 0)"
                },
                ["moves"] = new ToolProperty
                {
                    Type = "string",
                    Description = "Comma-separated list of moves (optional, e.g., 'tackle,growl')"
                }
            },
            Required = new List<string> { "nickname", "species", "level", "caughtAt" }
        }
    };

    public static readonly ToolDefinition MarkAsDead = new()
    {
        Name = "mark_as_dead",
        Description = "Mark a Pokemon as dead and move it to the graveyard (Nuzlocke rule: fainted Pokemon are considered dead)",
        Parameters = new ToolParameters
        {
            Type = "object",
            Properties = new Dictionary<string, ToolProperty>
            {
                ["nickname"] = new ToolProperty
                {
                    Type = "string",
                    Description = "Nickname of the Pokemon that died"
                },
                ["deathLocation"] = new ToolProperty
                {
                    Type = "string",
                    Description = "Location where the Pokemon died"
                },
                ["causeOfDeath"] = new ToolProperty
                {
                    Type = "string",
                    Description = "Cause of death (e.g., 'Defeated by Gym Leader Brock\\'s Onix')"
                }
            },
            Required = new List<string> { "nickname", "deathLocation", "causeOfDeath" }
        }
    };

    public static readonly ToolDefinition MoveToPC = new()
    {
        Name = "move_to_pc",
        Description = "Move a Pokemon from the team to PC storage",
        Parameters = new ToolParameters
        {
            Type = "object",
            Properties = new Dictionary<string, ToolProperty>
            {
                ["nickname"] = new ToolProperty
                {
                    Type = "string",
                    Description = "Nickname of the Pokemon to move to PC"
                }
            },
            Required = new List<string> { "nickname" }
        }
    };

    public static readonly ToolDefinition RecordEncounter = new()
    {
        Name = "record_encounter",
        Description = "Record an encounter at a specific location (Nuzlocke rule: only one Pokemon can be caught per route/location)",
        Parameters = new ToolParameters
        {
            Type = "object",
            Properties = new Dictionary<string, ToolProperty>
            {
                ["location"] = new ToolProperty
                {
                    Type = "string",
                    Description = "Name of the location/route (e.g., 'Route 1', 'Viridian Forest')"
                },
                ["capturedSpecies"] = new ToolProperty
                {
                    Type = "string",
                    Description = "Species caught at this location (null if none caught)"
                },
                ["capturedNickname"] = new ToolProperty
                {
                    Type = "string",
                    Description = "Nickname given to captured Pokemon (null if none caught)"
                }
            },
            Required = new List<string> { "location" }
        }
    };

    public static readonly ToolDefinition GetPokemon = new()
    {
        Name = "get_pokemon",
        Description = "Get detailed information about a Pokemon including stats, types, abilities, and moves. Use this to analyze Pokemon for strategic decisions.",
        Parameters = new ToolParameters
        {
            Type = "object",
            Properties = new Dictionary<string, ToolProperty>
            {
                ["nameOrId"] = new ToolProperty
                {
                    Type = "string",
                    Description = "Pokemon name or ID (e.g., 'pikachu', 'charizard', or '25')"
                }
            },
            Required = new List<string> { "nameOrId" }
        }
    };

    public static readonly ToolDefinition GetMove = new()
    {
        Name = "get_move",
        Description = "Get detailed information about a move including power, accuracy, type, and effects. Use this to evaluate move effectiveness.",
        Parameters = new ToolParameters
        {
            Type = "object",
            Properties = new Dictionary<string, ToolProperty>
            {
                ["nameOrId"] = new ToolProperty
                {
                    Type = "string",
                    Description = "Move name or ID (e.g., 'thunderbolt', 'surf', or '85')"
                }
            },
            Required = new List<string> { "nameOrId" }
        }
    };

    public static readonly ToolDefinition GetType = new()
    {
        Name = "get_type",
        Description = "Get type effectiveness information including super effective, not very effective, and immune matchups. Use this for battle strategy.",
        Parameters = new ToolParameters
        {
            Type = "object",
            Properties = new Dictionary<string, ToolProperty>
            {
                ["nameOrId"] = new ToolProperty
                {
                    Type = "string",
                    Description = "Type name or ID (e.g., 'fire', 'water', 'psychic', or '10')"
                }
            },
            Required = new List<string> { "nameOrId" }
        }
    };

    public static readonly ToolDefinition GetAbility = new()
    {
        Name = "get_ability",
        Description = "Get detailed information about an ability including effects and description. Use this to understand Pokemon abilities.",
        Parameters = new ToolParameters
        {
            Type = "object",
            Properties = new Dictionary<string, ToolProperty>
            {
                ["nameOrId"] = new ToolProperty
                {
                    Type = "string",
                    Description = "Ability name or ID (e.g., 'overgrow', 'torrent', or '65')"
                }
            },
            Required = new List<string> { "nameOrId" }
        }
    };

    public static readonly ToolDefinition GetItem = new()
    {
        Name = "get_item",
        Description = "Get detailed information about an item including category, effects, cost, and sprite. Useful for pokeballs, medicines, berries, and held items.",
        Parameters = new ToolParameters
        {
            Type = "object",
            Properties = new Dictionary<string, ToolProperty>
            {
                ["nameOrId"] = new ToolProperty
                {
                    Type = "string",
                    Description = "Item name or ID (e.g., 'poke-ball', 'potion', 'oran-berry', or '1')"
                }
            },
            Required = new List<string> { "nameOrId" }
        }
    };

    public static readonly ToolDefinition StartBattle = new()
    {
        Name = "start_battle",
        Description = "Start a new battle. Clears any previous battle context and creates a fresh one. Call this when the user enters combat.",
        Parameters = new ToolParameters
        {
            Type = "object",
            Properties = new Dictionary<string, ToolProperty>
            {
                ["opponentName"] = new ToolProperty
                {
                    Type = "string",
                    Description = "Name of the opponent (e.g., 'Gym Leader Brock', 'Wild Geodude', 'Rival Blue')"
                },
                ["activePokemonNickname"] = new ToolProperty
                {
                    Type = "string",
                    Description = "Nickname of the Pokemon the player is leading with (optional)"
                },
                ["battleType"] = new ToolProperty
                {
                    Type = "string",
                    Description = "Type of battle: 'wild', 'trainer', 'gym_leader', 'rival', 'elite_four' (optional)"
                }
            },
            Required = new List<string> { "opponentName" }
        }
    };

    public static readonly ToolDefinition AddBattleLog = new()
    {
        Name = "add_battle_log",
        Description = "Add a log entry to the current battle. Use this to record important battle events (damage dealt, switches, items used, etc.)",
        Parameters = new ToolParameters
        {
            Type = "object",
            Properties = new Dictionary<string, ToolProperty>
            {
                ["logEntry"] = new ToolProperty
                {
                    Type = "string",
                    Description = "Description of the battle event (e.g., 'Sparky used Thunderbolt on Onix - not very effective')"
                }
            },
            Required = new List<string> { "logEntry" }
        }
    };

    public static readonly ToolDefinition EndBattle = new()
    {
        Name = "end_battle",
        Description = "End the current battle and clear the battle context. Call this when the battle concludes.",
        Parameters = new ToolParameters
        {
            Type = "object",
            Properties = new Dictionary<string, ToolProperty>(),
            Required = new List<string>()
        }
    };

    public static IEnumerable<ToolDefinition> AllTools => new[]
    {
        GetGameState,
        AddToTeam,
        MarkAsDead,
        MoveToPC,
        RecordEncounter,
        StartBattle,
        AddBattleLog,
        EndBattle
    };
}

public class ToolDefinition
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required ToolParameters Parameters { get; set; }
}

public class ToolParameters
{
    public required string Type { get; set; }
    public required Dictionary<string, ToolProperty> Properties { get; set; }
    public required List<string> Required { get; set; }
}

public class ToolProperty
{
    public required string Type { get; set; }
    public required string Description { get; set; }
}
