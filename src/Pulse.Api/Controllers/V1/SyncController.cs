using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pulse.Application.Abstractions;
using Pulse.Application.Sync;

namespace Pulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/sync")]
[Authorize]
public sealed class SyncController(ISyncService sync) : ControllerBase
{
    [HttpPost("productos")]
    public async Task<ActionResult<SyncBatchResponse>> PushProductos([FromBody] ProductosSyncRequest body, CancellationToken ct)
    {
        var tid = TenantId();
        var result = await sync.PushProductosAsync(tid, body, ct);
        return Ok(result);
    }

    [HttpPost("clientes")]
    public async Task<ActionResult<SyncBatchResponse>> PushClientes([FromBody] ClientesSyncRequest body, CancellationToken ct)
    {
        var tid = TenantId();
        var result = await sync.PushClientesAsync(tid, body, ct);
        return Ok(result);
    }

    [HttpPost("ventas")]
    public async Task<ActionResult<SyncBatchResponse>> PushVentas([FromBody] VentasSyncRequest body, CancellationToken ct)
    {
        var tid = TenantId();
        var result = await sync.PushVentasAsync(tid, body, ct);
        return Ok(result);
    }

    [HttpPost("cobros")]
    public async Task<ActionResult<SyncBatchResponse>> PushCobros([FromBody] CobrosSyncRequest body, CancellationToken ct)
    {
        var tid = TenantId();
        var result = await sync.PushCobrosAsync(tid, body, ct);
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
