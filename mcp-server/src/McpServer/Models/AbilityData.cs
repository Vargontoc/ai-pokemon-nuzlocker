using System.Text.Json.Serialization;

namespace es.vargontoc.nuzlocke.ai.Models;

public class AbilityData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("is_main_series")]
    public bool IsMainSeries { get; set; }

    [JsonPropertyName("effect_entries")]
    public List<AbilityEffectEntry> EffectEntries { get; set; } = new();
}

public class AbilityEffectEntry
{
    [JsonPropertyName("effect")]
    public string Effect { get; set; } = string.Empty;

    [JsonPropertyName("short_effect")]
    public string ShortEffect { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public NamedApiResource Language { get; set; } = new();
}
