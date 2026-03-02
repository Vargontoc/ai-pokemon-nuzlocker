using System.Text.Json.Serialization;

namespace es.vargontoc.nuzlocke.ai.Models;

public class NuzlockeSessionInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Estado actual de la partida.</summary>
    public NuzlockeStatus Status { get; set; } = NuzlockeStatus.Active;

    /// <summary>Última vez que se guardó el estado de la partida.</summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>Generación del juego (sincronizado desde NuzlockeState).</summary>
    public int Generation { get; set; } = 1;

    /// <summary>Variante de Nuzlocke (sincronizado desde NuzlockeState).</summary>
    public string LockeType { get; set; } = "standard";
}

public class NuzlockeFileData
{
    public string SessionId { get; set; } = string.Empty;
    public NuzlockeState GameState { get; set; } = new();
    public BattleContext BattleContext { get; set; } = new();
    public AgentMemory AgentMemory { get; set; } = new();
}

public class BattleContext
{
    public bool InBattle { get; set; }
    public string? OpponentName { get; set; }
    public string? ActivePokemonNickname { get; set; }
    public int TurnCount { get; set; }
    public List<string> BattleLog { get; set; } = new();
    public DateTime? BattleStartedAt { get; set; }
    public string? BattleType { get; set; }
}

public class AgentMemory
{
    public List<string> KeyDecisions { get; set; } = new();
    public List<string> ConversationSummaries { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class NuzlockeRegistry
{
    public List<NuzlockeSessionInfo> Sessions { get; set; } = new();
}
