using es.vargontoc.nuzlocke.ai.Models;

namespace es.vargontoc.nuzlocke.ai.Services;

/// <summary>
/// Interfaz para gestionar el estado de la partida Nuzlocke
/// </summary>
public interface IStateManager
{
    /// <summary>
    /// Obtiene el estado actual del juego
    /// </summary>
    Task<NuzlockeState> GetStateAsync();

    // Multi-session variants
    Task<NuzlockeState> GetStateAsync(string sessionId);

    /// <summary>
    /// Guarda el estado actual
    /// </summary>
    Task SaveStateAsync(NuzlockeState state);

    Task SaveStateAsync(string sessionId, NuzlockeState state);

    /// <summary>
    /// Agrega un Pokemon al equipo (máximo 6)
    /// </summary>
    Task<bool> AddToTeamAsync(TeamMember pokemon);

    Task<bool> AddToTeamAsync(string sessionId, TeamMember pokemon);

    /// <summary>
    /// Marca un Pokemon como muerto y lo mueve al cementerio
    /// </summary>
    Task<bool> MarkAsDeadAsync(string nickname, string deathLocation, string causeOfDeath);

    Task<bool> MarkAsDeadAsync(string sessionId, string nickname, string deathLocation, string causeOfDeath);

    /// <summary>
    /// Mueve un Pokemon del equipo al PC
    /// </summary>
    Task<bool> MoveToPCAsync(string nickname);

    Task<bool> MoveToPCAsync(string sessionId, string nickname);

    /// <summary>
    /// Registra un encuentro en una ubicación
    /// </summary>
    Task<bool> RecordEncounterAsync(string location, string? capturedSpecies = null, string? capturedNickname = null);

    Task<bool> RecordEncounterAsync(string sessionId, string location, string? capturedSpecies = null, string? capturedNickname = null);
}
