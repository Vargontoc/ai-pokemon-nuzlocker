using System.Text.Json.Serialization;

namespace es.vargontoc.nuzlocke.ai.Models;

public class MoveData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("accuracy")]
    public int? Accuracy { get; set; }

    [JsonPropertyName("power")]
    public int? Power { get; set; }

    [JsonPropertyName("pp")]
    public int Pp { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("type")]
    public NamedApiResource Type { get; set; } = new();

    [JsonPropertyName("damage_class")]
    public NamedApiResource DamageClass { get; set; } = new();

    [JsonPropertyName("effect_entries")]
    public List<MoveEffectEntry> EffectEntries { get; set; } = new();
}

public class MoveEffectEntry
{
    [JsonPropertyName("effect")]
    public string Effect { get; set; } = string.Empty;

    [JsonPropertyName("short_effect")]
    public string ShortEffect { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public NamedApiResource Language { get; set; } = new();
}
