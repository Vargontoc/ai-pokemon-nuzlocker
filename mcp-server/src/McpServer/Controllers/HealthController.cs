using Microsoft.AspNetCore.Mvc;

namespace es.vargontoc.nuzlocke.ai.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/")]
    public IActionResult Get() => Ok("AI Pokemon Nuzlocker MCP Server");
}
