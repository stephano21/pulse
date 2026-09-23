using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pulse.Application.Abstractions;
using Pulse.Application.Sync;
using Pulse.Infrastructure.Identity;

namespace Pulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/ventas")]
[Authorize]
public sealed class VentasController(ISyncService sync, IAdminService admin) : ControllerBase
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

    /// <summary>
    /// Reversa (total o parcialmente) una venta desde la app. Vendedor: solo puede reversar sus
    /// propias ventas. Gerente: sin acceso (es de solo lectura). Dueño/SuperAdmin: cualquier venta
    /// del tenant.
    /// </summary>
    [HttpPost("{ventaId:guid}/reverso")]
    public async Task<IActionResult> Reversar(Guid ventaId, [FromBody] ReversarVentaRequest body, CancellationToken ct)
    {
        var isDueno = User.IsInRole(Roles.Dueno);
        var isSuperAdmin = User.IsInRole(Roles.SuperAdmin);
        var isGerente = User.IsInRole(Roles.Gerente);
        if (isGerente && !isDueno && !isSuperAdmin)
            return Forbid();

        var isVendedorOnly = User.IsInRole(Roles.Vendedor) && !isDueno && !isSuperAdmin && !isGerente;
        var scope = isVendedorOnly ? UserId() : (Guid?)null;

        try
        {
            var reverso = await admin.ReversarVentaAsync(TenantId(), ventaId, UserId(), scope, body, ct);
            return Ok(reverso);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException e)
        {
            return Problem(title: "No se pudo reversar la venta", detail: e.Message, statusCode: StatusCodes.Status400BadRequest);
        }
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
