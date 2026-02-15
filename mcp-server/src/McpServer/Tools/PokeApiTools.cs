using System.ComponentModel;
using System.Text.Json;
using es.vargontoc.nuzlocke.ai.Connectors;
using ModelContextProtocol.Server;

namespace es.vargontoc.nuzlocke.ai.Tools;

[McpServerToolType]
public static class PokeApiTools
{
    [McpServerTool]
    [Description("Get optimized information about a Pokemon by name or ID. Returns stats, types, and key moves (subset for token efficiency).")]
    public static async Task<string> GetPokemon(
        IPokeApiConnector connector,
        [Description("The name or ID of the Pokemon (e.g., 'pikachu' or '25')")] string nameOrId)
    {
        var pokemon = await connector.GetPokemonSubsetAsync(nameOrId);

        if (pokemon == null)
        {
            return JsonSerializer.Serialize(new { error = $"Pokemon '{nameOrId}' not found" });
        }

        return JsonSerializer.Serialize(pokemon, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    [McpServerTool]
    [Description("Get detailed information about a move by name or ID. Returns power, accuracy, type, and effects.")]
    public static async Task<string> GetMove(
        IPokeApiConnector connector,
        [Description("The name or ID of the move (e.g., 'thunderbolt' or '85')")] string nameOrId)
    {
        var move = await connector.GetMoveAsync(nameOrId);

        if (move == null)
        {
            return JsonSerializer.Serialize(new { error = $"Move '{nameOrId}' not found" });
        }

        return JsonSerializer.Serialize(move, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    [McpServerTool]
    [Description("Get type effectiveness information. Returns damage relations (super effective, not very effective, immune).")]
    public static async Task<string> GetType(
        IPokeApiConnector connector,
        [Description("The name or ID of the type (e.g., 'fire' or '10')")] string nameOrId)
    {
        var type = await connector.GetTypeAsync(nameOrId);

        if (type == null)
        {
            return JsonSerializer.Serialize(new { error = $"Type '{nameOrId}' not found" });
        }

        return JsonSerializer.Serialize(type, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    [McpServerTool]
    [Description("Get detailed information about an ability by name or ID. Returns effects and description.")]
    public static async Task<string> GetAbility(
        IPokeApiConnector connector,
        [Description("The name or ID of the ability (e.g., 'overgrow' or '65')")] string nameOrId)
    {
        var ability = await connector.GetAbilityAsync(nameOrId);

        if (ability == null)
        {
            return JsonSerializer.Serialize(new { error = $"Ability '{nameOrId}' not found" });
        }

        return JsonSerializer.Serialize(ability, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    [McpServerTool]
    [Description("Get detailed information about an item by name or ID. Returns category, effects, cost, and sprite. Useful for pokeballs, medicines, berries, and held items.")]
    public static async Task<string> GetItem(
        IPokeApiConnector connector,
        [Description("The name or ID of the item (e.g., 'poke-ball', 'potion', or '1')")] string nameOrId)
    {
        var item = await connector.GetItemAsync(nameOrId);

        if (item == null)
        {
            return JsonSerializer.Serialize(new { error = $"Item '{nameOrId}' not found" });
        }

        return JsonSerializer.Serialize(item, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
