namespace es.vargontoc.nuzlocke.ai.Models;

/// <summary>
/// Metadata de una partida Nuzlocke. Se persiste en SQLite y también como copia en el fichero .nuzlocke.
/// </summary>
public class NuzlockeMetadata
{
    /// <summary>UUID único de la partida (PK).</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Nombre descriptivo de la partida (max 50 caracteres).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Descripción opcional de la partida (max 100 caracteres).</summary>
    public string? Descripcion { get; set; }

    /// <summary>Generación del juego (1 por ahora).</summary>
    public int Generation { get; set; } = 1;

    /// <summary>Tipo de reglas del nuzlocke.</summary>
    public LockeType LockeType { get; set; } = LockeType.Standard;

    /// <summary>
    /// True una vez que InitNuzlockeWorkflow ha corrido con éxito para esta partida.
    /// False significa que la sesión fue creada pero todavía no inicializada.
    /// </summary>
    public bool IsInitialized { get; set; } = false;

    /// <summary>Estado actual de la partida. Building = recién creada, no inicializada.</summary>
    public NuzlockeStatus Status { get; set; } = NuzlockeStatus.Building;

    /// <summary>Fecha de creación de la partida.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Última vez que se actualizó la metadata.</summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
