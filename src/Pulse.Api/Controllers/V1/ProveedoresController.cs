using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pulse.Application.Abstractions;

namespace Pulse.Api.Controllers.V1;

/// <summary>Pull de proveedores, compras a crédito y pagos a proveedores (el push va por /v1/sync/*).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}")]
[Authorize]
public sealed class ProveedoresController(ISyncService sync) : ControllerBase
{
    [HttpGet("proveedores")]
    public async Task<IActionResult> Proveedores(
        [FromQuery(Name = "updated_since")] DateTimeOffset? updatedSince,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var result = await sync.PullProveedoresAsync(TenantId(), updatedSince, cursor, limit, ct);
        return Ok(result);
    }

    [HttpGet("compras-proveedor")]
    public async Task<IActionResult> Compras(
        [FromQuery(Name = "created_since")] DateTimeOffset? createdSince,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var result = await sync.PullComprasProveedorAsync(TenantId(), createdSince, cursor, limit, ct);
        return Ok(result);
    }

    [HttpGet("pagos-proveedor")]
    public async Task<IActionResult> Pagos(
        [FromQuery(Name = "created_since")] DateTimeOffset? createdSince,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var result = await sync.PullPagosProveedorAsync(TenantId(), createdSince, cursor, limit, ct);
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
