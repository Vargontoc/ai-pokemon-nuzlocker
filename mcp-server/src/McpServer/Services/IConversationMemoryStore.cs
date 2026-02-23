using es.vargontoc.nuzlocke.ai.Models;

namespace es.vargontoc.nuzlocke.ai.Services;

/// <summary>
/// Persists conversation history per nuzlocke session.
/// Each entry is stored in {nuzlockePath}/memory/conversation.json.
/// </summary>
public interface IConversationMemoryStore
{
    /// <summary>
    /// Loads all conversation entries for the given session.
    /// Returns an empty list if no history exists or the session cannot be resolved.
    /// </summary>
    Task<List<ConversationEntry>> LoadAsync(string sessionId);

    /// <summary>
    /// Appends a single entry to the conversation history for the given session.
    /// </summary>
    Task AppendAsync(string sessionId, ConversationEntry entry);
}
