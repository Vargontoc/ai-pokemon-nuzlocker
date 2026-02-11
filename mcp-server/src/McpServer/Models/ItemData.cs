using System.Text.Json.Serialization;

namespace es.vargontoc.nuzlocke.ai.Models;

public class ItemData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("cost")]
    public int Cost { get; set; }

    [JsonPropertyName("category")]
    public ItemCategory Category { get; set; } = new();

    [JsonPropertyName("effect_entries")]
    public List<ItemEffectEntry> EffectEntries { get; set; } = new();

    [JsonPropertyName("sprites")]
    public ItemSprites Sprites { get; set; } = new();

    [JsonPropertyName("attributes")]
    public List<NamedApiResource> Attributes { get; set; } = new();

    [JsonPropertyName("fling_power")]
    public int? FlingPower { get; set; }

    [JsonPropertyName("fling_effect")]
    public NamedApiResource? FlingEffect { get; set; }
}

public class ItemCategory
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public class ItemEffectEntry
{
    [JsonPropertyName("effect")]
    public string Effect { get; set; } = string.Empty;

    [JsonPropertyName("short_effect")]
    public string ShortEffect { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public NamedApiResource Language { get; set; } = new();
}

public class ItemSprites
{
    [JsonPropertyName("default")]
    public string? Default { get; set; }
}
