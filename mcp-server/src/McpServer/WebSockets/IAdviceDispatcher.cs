namespace es.vargontoc.nuzlocke.ai.WebSockets;

/// <summary>
/// Dispatches advice generation as a background task.
/// Streams LLM response tokens via WebSocket to the connected client.
/// </summary>
public interface IAdviceDispatcher
{
    /// <summary>
    /// Fire-and-forget: spawns a background task that calls IAiProvider.StreamCompletionAsync
    /// and sends advice_start/advice_chunk/advice_end messages via WebSocket.
    /// </summary>
    void Dispatch(AdviceDispatchRequest request);

    /// <summary>
    /// Fire-and-forget: spawns a background task that runs the full NuzlockeAgent tool-calling loop
    /// and sends the final advice via WebSocket.
    /// </summary>
    void DispatchAgentAdvice(AgentAdviceDispatchRequest request);
}
