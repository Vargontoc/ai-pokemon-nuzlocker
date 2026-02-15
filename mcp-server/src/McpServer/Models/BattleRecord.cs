namespace es.vargontoc.nuzlocke.ai.Models;

/// <summary>
/// Registro histórico de una batalla completada.
/// Schema diseñado para futuro entrenamiento ML.
/// </summary>
public class BattleRecord
{
    public string OpponentName { get; set; } = string.Empty;
    public string BattleType { get; set; } = string.Empty;
    public int TurnCount { get; set; }

    /// <summary>
    /// Resultado: "won", "lost", "fled"
    /// </summary>
    public string Outcome { get; set; } = string.Empty;

    /// <summary>
    /// Nicknames de los pokemon usados en la batalla
    /// </summary>
    public List<string> PokemonUsed { get; set; } = new();

    /// <summary>
    /// Nicknames de los pokemon perdidos (muertos) en la batalla
    /// </summary>
    public List<string> PokemonLost { get; set; } = new();

    /// <summary>
    /// Consejo que dio el LLM durante la batalla
    /// </summary>
    public string AdviceGiven { get; set; } = string.Empty;

    /// <summary>
    /// Log completo de la batalla
    /// </summary>
    public List<string> BattleLog { get; set; } = new();

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
