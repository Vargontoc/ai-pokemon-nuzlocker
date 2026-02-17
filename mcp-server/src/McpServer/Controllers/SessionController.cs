using es.vargontoc.nuzlocke.ai.Models;
using es.vargontoc.nuzlocke.ai.Services;
using Microsoft.AspNetCore.Mvc;

namespace es.vargontoc.nuzlocke.ai.Controllers;

[ApiController]
[Route("nuzlocke/sessions")]
public class SessionController : ControllerBase
{
    [HttpPost]
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

    [HttpGet]
    public async Task<IActionResult> ListSessions(
        [FromServices] INuzlockeSessionManager sessions)
    {
        return Ok(await sessions.ListSessionsAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSession(
        string id,
        [FromServices] INuzlockeSessionManager sessions)
    {
        var s = await sessions.GetSessionAsync(id);
        return s == null ? NotFound() : Ok(s);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSession(
        string id,
        [FromServices] INuzlockeSessionManager sessions)
    {
        var ok = await sessions.DeleteSessionAsync(id);
        return ok ? NoContent() : NotFound();
    }

    [HttpGet("{id}/data")]
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

    [HttpPost("{id}/data")]
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
}
