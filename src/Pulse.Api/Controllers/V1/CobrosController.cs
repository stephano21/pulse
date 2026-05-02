using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pulse.Application.Abstractions;

namespace Pulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/cobros")]
[Authorize]
public sealed class CobrosController(ISyncService sync) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Pull(
        [FromQuery(Name = "created_since")] DateTimeOffset? createdSince,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var tid = TenantId();
        var result = await sync.PullCobrosAsync(tid, createdSince, cursor, limit, ct);
        return Ok(result);
    }

    private Guid TenantId()
    {
        var v = User.FindFirst("tenant_id")?.Value;
        if (v == null || !Guid.TryParse(v, out var g))
            throw new InvalidOperationException("Falta claim tenant_id.");
        return g;
    }
}
