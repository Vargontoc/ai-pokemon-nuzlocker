using es.vargontoc.nuzlocke.ai.Models;

namespace es.vargontoc.nuzlocke.ai.Services;

/// <summary>
/// Provides the RESPONSE STYLE instruction block for each agent personality.
/// The block is injected at the end of the system prompt, after all workflow rules.
/// </summary>
public interface IPersonalityPromptProvider
{
    /// <summary>
    /// Returns the personality instruction block to append to the system prompt.
    /// </summary>
    string GetPersonalityBlock(AgentPersonality personality);
}
