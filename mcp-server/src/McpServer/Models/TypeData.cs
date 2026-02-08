using System.Text.Json.Serialization;

namespace es.vargontoc.nuzlocke.ai.Models;

public class TypeData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("damage_relations")]
    public TypeDamageRelations DamageRelations { get; set; } = new();
}

public class TypeDamageRelations
{
    [JsonPropertyName("double_damage_from")]
    public List<NamedApiResource> DoubleDamageFrom { get; set; } = new();

    [JsonPropertyName("double_damage_to")]
    public List<NamedApiResource> DoubleDamageTo { get; set; } = new();

    [JsonPropertyName("half_damage_from")]
    public List<NamedApiResource> HalfDamageFrom { get; set; } = new();

    [JsonPropertyName("half_damage_to")]
    public List<NamedApiResource> HalfDamageTo { get; set; } = new();

    [JsonPropertyName("no_damage_from")]
    public List<NamedApiResource> NoDamageFrom { get; set; } = new();

    [JsonPropertyName("no_damage_to")]
    public List<NamedApiResource> NoDamageTo { get; set; } = new();
}
