using es.vargontoc.nuzlocke.ai.Models;

namespace es.vargontoc.nuzlocke.ai.Services;

public interface IPokeApiConnector
{
    /// <summary>
    /// Obtiene información de un Pokémon por su nombre o ID
    /// </summary>
    Task<PokemonData?> GetPokemonAsync(string nameOrId);

    /// <summary>
    /// Obtiene información de un movimiento por su nombre o ID
    /// </summary>
    Task<MoveData?> GetMoveAsync(string nameOrId);

    /// <summary>
    /// Obtiene información de un tipo por su nombre o ID
    /// </summary>
    Task<TypeData?> GetTypeAsync(string nameOrId);

    /// <summary>
    /// Obtiene información de una habilidad por su nombre o ID
    /// </summary>
    Task<AbilityData?> GetAbilityAsync(string nameOrId);
}
