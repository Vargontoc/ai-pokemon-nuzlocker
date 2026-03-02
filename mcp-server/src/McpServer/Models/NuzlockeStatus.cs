namespace es.vargontoc.nuzlocke.ai.Models;

/// <summary>
/// Estado de una partida Nuzlocke.
/// </summary>
public enum NuzlockeStatus
{
    /// <summary>Partida en curso.</summary>
    Active,

    /// <summary>El jugador completó la Liga Pokémon (victoria).</summary>
    Finished,

    /// <summary>Todos los Pokémon murieron — el Nuzlocke terminó en derrota.</summary>
    GameOver,

    /// <summary>El jugador abandonó la partida manualmente.</summary>
    Abandoned
}
