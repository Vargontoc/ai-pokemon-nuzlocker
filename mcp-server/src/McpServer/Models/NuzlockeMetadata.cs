namespace es.vargontoc.nuzlocke.ai.Models;

/// <summary>
/// Metadata almacenada en el fichero .nuzlocke de cada partida.
/// Contiene información inmutable de la partida.
/// </summary>
public class NuzlockeMetadata
{
    /// <summary>
    /// Identificador único: GUID corto + fecha (ej: a3f1b2c4_2026-02-15)
    /// </summary>
    public string NuzlockeId { get; set; } = string.Empty;

    /// <summary>
    /// Generación del juego (1 por ahora)
    /// </summary>
    public int Generation { get; set; } = 1;

    /// <summary>
    /// Tipo de nuzlocke (standard, hardcore, etc.)
    /// </summary>
    public string LockeType { get; set; } = "standard";

    /// <summary>
    /// Fecha de creación de la partida
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ruta base donde se encuentra la carpeta del nuzlocke
    /// </summary>
    public string BasePath { get; set; } = string.Empty;
}
