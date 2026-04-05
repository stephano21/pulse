using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Pulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}")]
public sealed class HealthController : ControllerBase
{
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Get() => Ok(new { status = "ok" });
}
