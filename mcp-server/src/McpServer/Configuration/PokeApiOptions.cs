namespace es.vargontoc.nuzlocke.ai.Configuration;

public class PokeApiOptions
{
    public const string SectionName = "PokeApi";

    public string BaseUrl { get; set; } = "https://pokeapi.co/api/v2";
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// TTL en minutos para la caché en memoria (L1). 0 desactiva L1.
    /// </summary>
    public int MemoryCacheTtlMinutes { get; set; } = 60;
}
