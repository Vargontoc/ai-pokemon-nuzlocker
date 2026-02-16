using es.vargontoc.nuzlocke.ai.Models;

namespace es.vargontoc.nuzlocke.ai.Services;

/// <summary>
/// Gestiona operaciones de fichero por nuzlocke individual.
/// Cada nuzlocke tiene su propia carpeta con estructura:
///   {basePath}/{nuzlockeId}/
///     ├── .nuzlocke          ← metadata (id, generation, locke_type, created_at)
///     ├── game_state.json    ← NuzlockeState
///     ├── battle_state.json  ← temporal, solo durante batalla activa
///     ├── memory/            ← contexto de memoria del agente
///     └── results/           ← BattleRecords individuales
/// </summary>
public interface INuzlockeFileManager
{
    /// <summary>
    /// Carga el registry de nuzlockes desde SQLite al cache en memoria.
    /// Debe llamarse al iniciar la aplicación.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Crea una nueva partida nuzlocke con su estructura de carpetas completa.
    /// </summary>
    /// <returns>NuzlockeId generado (GUID + fecha)</returns>
    Task<string> CreateNuzlockeAsync(string basePath, int generation, string lockeType);

    /// <summary>
    /// Carga el game_state.json de un nuzlocke.
    /// </summary>
    Task<NuzlockeState> LoadGameStateAsync(string nuzlockeId);

    /// <summary>
    /// Guarda el game_state.json de un nuzlocke.
    /// </summary>
    Task SaveGameStateAsync(string nuzlockeId, NuzlockeState state);

    /// <summary>
    /// Carga el battle_state.json temporal (null si no existe batalla activa).
    /// </summary>
    Task<BattleContext?> LoadBattleStateAsync(string nuzlockeId);

    /// <summary>
    /// Guarda el battle_state.json temporal.
    /// </summary>
    Task SaveBattleStateAsync(string nuzlockeId, BattleContext battle);

    /// <summary>
    /// Elimina el battle_state.json temporal al finalizar una batalla.
    /// </summary>
    Task DeleteBattleStateAsync(string nuzlockeId);

    /// <summary>
    /// Guarda un BattleRecord individual en results/ con nombre secuencial.
    /// </summary>
    Task SaveBattleRecordAsync(string nuzlockeId, BattleRecord record);

    /// <summary>
    /// Lista todos los nuzlockes activos en un basePath leyendo .nuzlocke de cada carpeta.
    /// </summary>
    Task<List<NuzlockeMetadata>> ListNuzlockesAsync(string basePath);

    /// <summary>
    /// Lee la metadata desde el fichero .nuzlocke de un nuzlocke.
    /// </summary>
    Task<NuzlockeMetadata?> GetNuzlockeMetadataAsync(string nuzlockeId);

    /// <summary>
    /// Resuelve la ruta completa de un nuzlockeId desde cache en memoria.
    /// Devuelve null si no está en cache.
    /// </summary>
    string? GetNuzlockePath(string nuzlockeId);

    /// <summary>
    /// Resuelve la ruta completa de un nuzlockeId.
    /// Busca primero en cache L1 (memoria), luego en L2 (SQLite).
    /// </summary>
    Task<string?> GetNuzlockePathAsync(string nuzlockeId);
}
