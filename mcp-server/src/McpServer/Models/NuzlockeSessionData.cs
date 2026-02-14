using System.Text.Json.Serialization;

namespace es.vargontoc.nuzlocke.ai.Models;

public class NuzlockeSessionInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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
