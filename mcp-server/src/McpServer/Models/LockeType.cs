namespace es.vargontoc.nuzlocke.ai.Models;

/// <summary>
/// Tipo de reglas del Nuzlocke.
/// </summary>
public enum LockeType
{
    /// <summary>Reglas estándar de Nuzlocke (primera captura por ruta, muerte permanente).</summary>
    Standard,

    /// <summary>Reglas hardcore: mismas que standard + sin centros Pokémon.</summary>
    Hardcore
}
