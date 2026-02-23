namespace es.vargontoc.nuzlocke.ai.Models;

public class ConversationEntry
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
