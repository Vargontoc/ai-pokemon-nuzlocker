using System.Text.Json.Serialization;

namespace es.vargontoc.nuzlocke.ai.Models;

public class PokemonData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("weight")]
    public int Weight { get; set; }

    [JsonPropertyName("base_experience")]
    public int BaseExperience { get; set; }

    [JsonPropertyName("types")]
    public List<PokemonTypeSlot> Types { get; set; } = new();

    [JsonPropertyName("abilities")]
    public List<PokemonAbilitySlot> Abilities { get; set; } = new();

    [JsonPropertyName("stats")]
    public List<PokemonStatSlot> Stats { get; set; } = new();

    [JsonPropertyName("moves")]
    public List<PokemonMoveSlot> Moves { get; set; } = new();
}

public class PokemonTypeSlot
{
    [JsonPropertyName("slot")]
    public int Slot { get; set; }

    [JsonPropertyName("type")]
    public NamedApiResource Type { get; set; } = new();
}

public class PokemonAbilitySlot
{
    [JsonPropertyName("is_hidden")]
    public bool IsHidden { get; set; }

    [JsonPropertyName("slot")]
    public int Slot { get; set; }

    [JsonPropertyName("ability")]
    public NamedApiResource Ability { get; set; } = new();
}

public class PokemonStatSlot
{
    [JsonPropertyName("base_stat")]
    public int BaseStat { get; set; }

    [JsonPropertyName("effort")]
    public int Effort { get; set; }

    [JsonPropertyName("stat")]
    public NamedApiResource Stat { get; set; } = new();
}

public class PokemonMoveSlot
{
    [JsonPropertyName("move")]
    public NamedApiResource Move { get; set; } = new();
}

public class NamedApiResource
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
