using System.Text.Json.Serialization;

namespace es.vargontoc.nuzlocke.ai.Models;

public class PokemonSubset
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("types")]
    public List<string> Types { get; set; } = new();

    [JsonPropertyName("stats")]
    public Dictionary<string, int> Stats { get; set; } = new();

    [JsonPropertyName("moves_basicos")]
    public List<string> MovesBasicos { get; set; } = new();

    [JsonPropertyName("sprite_min")]
    public string? SpriteMin { get; set; }
}
