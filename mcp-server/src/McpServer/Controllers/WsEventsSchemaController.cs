using es.vargontoc.nuzlocke.ai.WebSockets;
using Microsoft.AspNetCore.Mvc;

namespace es.vargontoc.nuzlocke.ai.Controllers;

/// <summary>
/// WebSocket event schemas — reference only.
/// These endpoints exist solely so Swagger registers and documents all WebSocket message types.
/// They are NOT intended to be called directly; all events are pushed server-side via the WS connection at <c>ws://host/ws/advice?nuzlockeId={id}</c>.
///
/// ## Connection
/// Connect to <c>/ws/advice?nuzlockeId={id}</c>. On connect you receive a <c>connected</c> event.
///
/// ## Event flow for <c>POST /nuzlocke/advice</c> (agent advice)
/// <code>
/// → advice_start
/// → agent_tool_call  (0..N, one per tool the agent calls)
/// → advice_end  (or advice_error on failure)
/// </code>
///
/// ## Event flow for <c>POST /nuzlocke/workflow</c> (gameplay workflows)
/// <code>
/// → workflow_start
/// → workflow_event  (result with mutations)
/// → advice_start  (if LLM advice is generated)
/// → advice_chunk  (0..N streaming chunks, only for stream path)
/// → advice_end  (or advice_error on failure)
/// </code>
/// </summary>
[ApiController]
[Route("ws/events")]
[Produces("application/json")]
[Tags("WebSocket Events")]
public class WsEventsSchemaController : ControllerBase
{
    /// <summary>
    /// Schema: <c>connected</c> — sent once when the WebSocket connection is established.
    /// </summary>
    [HttpGet("connected")]
    [ProducesResponseType(typeof(ConnectedMessage), StatusCodes.Status200OK)]
    public IActionResult Connected() => Ok();

    /// <summary>
    /// Schema: <c>advice_start</c> — sent when the LLM advice generation begins.
    /// The <c>workflowId</c> field identifies which workflow triggered the advice (or <c>"agent_advice"</c> for direct advice requests).
    /// </summary>
    [HttpGet("advice-start")]
    [ProducesResponseType(typeof(AdviceStartMessage), StatusCodes.Status200OK)]
    public IActionResult AdviceStart() => Ok();

    /// <summary>
    /// Schema: <c>advice_chunk</c> — a streaming text chunk from the LLM (only emitted in streaming paths).
    /// </summary>
    [HttpGet("advice-chunk")]
    [ProducesResponseType(typeof(AdviceChunkMessage), StatusCodes.Status200OK)]
    public IActionResult AdviceChunk() => Ok();

    /// <summary>
    /// Schema: <c>advice_end</c> — sent when advice generation completes successfully.
    /// Contains the full concatenated advice text.
    /// </summary>
    [HttpGet("advice-end")]
    [ProducesResponseType(typeof(AdviceEndMessage), StatusCodes.Status200OK)]
    public IActionResult AdviceEnd() => Ok();

    /// <summary>
    /// Schema: <c>advice_error</c> — sent when advice generation fails.
    /// </summary>
    [HttpGet("advice-error")]
    [ProducesResponseType(typeof(AdviceErrorMessage), StatusCodes.Status200OK)]
    public IActionResult AdviceError() => Ok();

    /// <summary>
    /// Schema: <c>workflow_start</c> — sent immediately before a workflow begins execution.
    /// Front can use this to show a loading indicator.
    /// </summary>
    [HttpGet("workflow-start")]
    [ProducesResponseType(typeof(WorkflowStartMessage), StatusCodes.Status200OK)]
    public IActionResult WorkflowStart() => Ok();

    /// <summary>
    /// Schema: <c>workflow_event</c> — sent when a workflow completes (success or failure).
    /// Contains the resulting state mutations and any associated data or errors.
    /// </summary>
    [HttpGet("workflow-event")]
    [ProducesResponseType(typeof(WorkflowEventMessage), StatusCodes.Status200OK)]
    public IActionResult WorkflowEvent() => Ok();

    /// <summary>
    /// Schema: <c>agent_tool_call</c> — sent each time the agent invokes a tool during an advice request.
    /// <c>source</c> values: <c>"database"</c> (game state), <c>"pokeapi"</c> (external PokeAPI), <c>"workflow"</c> (gameplay workflow).
    /// </summary>
    [HttpGet("agent-tool-call")]
    [ProducesResponseType(typeof(AgentToolCallMessage), StatusCodes.Status200OK)]
    public IActionResult AgentToolCall() => Ok();
}
