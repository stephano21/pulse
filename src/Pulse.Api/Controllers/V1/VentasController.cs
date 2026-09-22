using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pulse.Application.Abstractions;
using Pulse.Infrastructure.Identity;

namespace Pulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/ventas")]
[Authorize]
public sealed class VentasController(ISyncService sync) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Pull(
        [FromQuery(Name = "created_since")] DateTimeOffset? createdSince,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var tid = TenantId();
        // Un Vendedor solo ve lo suyo (+ fiado compartido); Dueño/Gerente/SuperAdmin ven todo el tenant.
        var scope = User.IsInRole(Roles.Vendedor) && !User.IsInRole(Roles.Dueno) && !User.IsInRole(Roles.Gerente) && !User.IsInRole(Roles.SuperAdmin)
            ? UserId()
            : (Guid?)null;
        var result = await sync.PullVentasAsync(tid, scope, createdSince, cursor, limit, ct);
        return Ok(result);
    }

    private Guid TenantId()
    {
        var v = User.FindFirst("tenant_id")?.Value;
        if (v == null || !Guid.TryParse(v, out var g))
            throw new InvalidOperationException("Falta claim tenant_id.");
        return g;
    }

    private Guid UserId()
    {
        var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (v == null || !Guid.TryParse(v, out var g))
            throw new InvalidOperationException("Falta claim de usuario.");
        return g;
    }
}
