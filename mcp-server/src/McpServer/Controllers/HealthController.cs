using Microsoft.AspNetCore.Mvc;

namespace es.vargontoc.nuzlocke.ai.Controllers;

/// <summary>Root endpoint — confirms the server is running.</summary>
[ApiController]
public class HealthController : ControllerBase
{
    /// <summary>Returns a plain text confirmation that the server is up.</summary>
    [HttpGet("/")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public IActionResult Get() => Ok("AI Pokemon Nuzlocker MCP Server");
}
