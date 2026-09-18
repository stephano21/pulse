using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pulse.Application.Abstractions;
using Pulse.Application.Admin;
using Pulse.Infrastructure.Identity;

namespace Pulse.Api.Controllers.V1.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/admin/tenants/{tenantId:guid}")]
[Authorize(Roles = Roles.SuperAdmin)]
public sealed class AdminTenantDataController(ISyncService sync, IAdminService admin) : ControllerBase
{
    [HttpGet("clientes")]
    public async Task<IActionResult> Clientes(
        Guid tenantId,
        [FromQuery(Name = "updated_since")] DateTimeOffset? updatedSince,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var result = await sync.PullClientesAsync(tenantId, updatedSince, cursor, limit, ct);
        return Ok(result);
    }

    [HttpGet("productos")]
    public async Task<IActionResult> Productos(
        Guid tenantId,
        [FromQuery(Name = "updated_since")] DateTimeOffset? updatedSince,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var result = await sync.PullProductosAsync(tenantId, updatedSince, cursor, limit, ct);
        return Ok(result);
    }

    [HttpGet("ventas")]
    public async Task<IActionResult> Ventas(
        Guid tenantId,
        [FromQuery(Name = "created_since")] DateTimeOffset? createdSince,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var result = await sync.PullVentasAsync(tenantId, createdSince, cursor, limit, ct);
        return Ok(result);
    }

    [HttpGet("cobros")]
    public async Task<IActionResult> Cobros(
        Guid tenantId,
        [FromQuery(Name = "created_since")] DateTimeOffset? createdSince,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var result = await sync.PullCobrosAsync(tenantId, createdSince, cursor, limit, ct);
        return Ok(result);
    }

    [HttpGet("unidades")]
    public async Task<IActionResult> Unidades(
        Guid tenantId,
        [FromQuery(Name = "updated_since")] DateTimeOffset? updatedSince,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var result = await sync.PullUnidadesAsync(tenantId, updatedSince, cursor, limit, ct);
        return Ok(result);
    }

    [HttpPut("productos/{productoId:guid}/stock")]
    public async Task<IActionResult> AjustarStock(Guid tenantId, Guid productoId, [FromBody] AdjustProductoStockRequest body, CancellationToken ct)
    {
        var producto = await admin.AdjustProductoStockAsync(tenantId, productoId, body.Stock, ct);
        return producto is null ? NotFound() : Ok(producto);
    }

    [HttpPut("clientes/{clienteId:guid}/saldo")]
    public async Task<IActionResult> AjustarSaldo(Guid tenantId, Guid clienteId, [FromBody] AdjustClienteSaldoRequest body, CancellationToken ct)
    {
        var cliente = await admin.AdjustClienteSaldoAsync(tenantId, clienteId, body.DeudaInicial, body.SaldoAFavor, ct);
        return cliente is null ? NotFound() : Ok(cliente);
    }

    [HttpPost("ventas")]
    public async Task<IActionResult> CrearVenta(Guid tenantId, [FromBody] AdminCreateVentaRequest body, CancellationToken ct)
    {
        var venta = await admin.CreateVentaAsync(tenantId, body, ct);
        return Ok(venta);
    }
}
