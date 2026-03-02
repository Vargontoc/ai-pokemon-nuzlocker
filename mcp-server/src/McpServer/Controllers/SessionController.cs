using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Services;
using Microsoft.AspNetCore.Mvc;

namespace es.vargontoc.nuzlocke.ai.Controllers;

/// <summary>
/// Manage Nuzlocke sessions (create, list, get, delete) and their persistent game data.
/// </summary>
[ApiController]
[Route("nuzlocke/sessions")]
[Produces("application/json")]
public class SessionController : ControllerBase
{
    /// <summary>
    /// Create a new Nuzlocke session.
    /// </summary>
    /// <remarks>
    /// Creates a directory-backed session with a unique ID.
    ///
    /// Example:
    ///
    ///     POST /nuzlocke/sessions
    ///     {
    ///       "name": "My FireRed Run",
    ///       "directoryPath": "C:/saves/firered"
    ///     }
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(NuzlockeSessionInfo), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSession(
        [FromBody] CreateSessionRequest request,
        [FromServices] INuzlockeSessionManager sessions)
    {
        try
        {
            var info = await sessions.CreateSessionAsync(request.Name, request.DirectoryPath);
            return Created($"/nuzlocke/sessions/{info.Id}", info);
        }
        catch (Exception ex)
        {
            return Problem(detail: ex.Message, statusCode: 400);
        }
    }

    /// <summary>
    /// List all existing Nuzlocke sessions.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<NuzlockeSessionInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSessions(
        [FromServices] INuzlockeSessionManager sessions)
    {
        return Ok(await sessions.ListSessionsAsync());
    }

    /// <summary>
    /// Get a specific Nuzlocke session by ID.
    /// </summary>
    /// <param name="id">The session ID (GUID string).</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(NuzlockeSessionInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSession(
        string id,
        [FromServices] INuzlockeSessionManager sessions)
    {
        var s = await sessions.GetSessionAsync(id);
        return s == null ? NotFound() : Ok(s);
    }

    /// <summary>
    /// Delete a Nuzlocke session and its associated data.
    /// </summary>
    /// <param name="id">The session ID (GUID string).</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSession(
        string id,
        [FromServices] INuzlockeSessionManager sessions)
    {
        var ok = await sessions.DeleteSessionAsync(id);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Load the full game data for a session (game state, battle context, agent memory).
    /// </summary>
    /// <param name="id">The session ID.</param>
    [HttpGet("{id}/data")]
    [ProducesResponseType(typeof(NuzlockeFileData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessionData(
        string id,
        [FromServices] INuzlockeSessionManager sessions)
    {
        try
        {
            var data = await sessions.LoadSessionDataAsync(id);
            return Ok(data);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Persist updated game data for a session.
    /// </summary>
    /// <param name="id">The session ID.</param>
    /// <param name="fileData">Full game data snapshot to save.</param>
    [HttpPost("{id}/data")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveSessionData(
        string id,
        [FromBody] NuzlockeFileData fileData,
        [FromServices] INuzlockeSessionManager sessions)
    {
        try
        {
            await sessions.SaveSessionDataAsync(id, fileData);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Update the status of a Nuzlocke session.
    /// </summary>
    /// <remarks>
    /// Use this endpoint when the player explicitly marks a run as finished, lost or abandoned.
    /// The status is stored in the session registry and returned on every list/get call
    /// without loading the full game state.
    ///
    /// Valid status values: `Active`, `Finished`, `GameOver`, `Abandoned`
    ///
    /// Example — mark a run as completed after beating the Elite Four:
    ///
    ///     PATCH /nuzlocke/sessions/abc123/status
    ///     {
    ///       "status": "Finished"
    ///     }
    ///
    /// Example — mark a run as lost after a blackout:
    ///
    ///     PATCH /nuzlocke/sessions/abc123/status
    ///     {
    ///       "status": "GameOver"
    ///     }
    /// </remarks>
    /// <param name="id">The session ID.</param>
    /// <param name="request">New status value.</param>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        string id,
        [FromBody] UpdateStatusRequest request,
        [FromServices] INuzlockeSessionManager sessions)
    {
        if (!Enum.TryParse<NuzlockeStatus>(request.Status, ignoreCase: true, out var status))
        {
            var valid = string.Join(", ", Enum.GetNames<NuzlockeStatus>());
            return BadRequest(new { error = $"Invalid status '{request.Status}'. Valid values: {valid}" });
        }

        var updated = await sessions.UpdateStatusAsync(id, status);
        return updated ? NoContent() : NotFound();
    }
}

/// <summary>Request body for PATCH /nuzlocke/sessions/{id}/status.</summary>
public record UpdateStatusRequest(string Status);
