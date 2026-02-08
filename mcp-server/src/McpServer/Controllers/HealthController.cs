using Microsoft.AspNetCore.Mvc;

namespace es.vargontoc.nuzlocke.ai.Controllers;

[ApiController]
[Route("api/nuzlocker")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok" });
}
