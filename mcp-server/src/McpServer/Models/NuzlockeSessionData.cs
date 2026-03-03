namespace es.vargontoc.nuzlocke.ai.Models;

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
