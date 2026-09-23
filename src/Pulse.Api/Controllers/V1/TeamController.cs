using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pulse.Application.Abstractions;
using Pulse.Application.Admin;
using Pulse.Application.Sync;
using Pulse.Infrastructure.Identity;

namespace Pulse.Api.Controllers.V1;

/// <summary>
/// Gestión del propio equipo y tenant (del que llama). A diferencia de /v1/admin/*, no requiere
/// SuperAdmin — pero sí requiere rol Dueño o Gerente en el propio tenant para casi todo acá
/// (Vendedor no tiene acceso web, solo a la app). Dueño ve y gestiona todo; Gerente solo lee
/// (sin costos/ganancia, sin gestión de vendedores, sin config ni ajustes manuales).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/team")]
[Authorize(Roles = Roles.TenantAdminRoles)]
public sealed class TeamController(IAdminService admin, ISyncService sync) : ControllerBase
{
    public sealed class CreateTeamUserRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        /// <summary>Dueno, Gerente o Vendedor.</summary>
        public string Role { get; set; } = "";
    }

    public sealed class SetTenantLogoRequest
    {
        /// <summary>Id devuelto por POST /v1/files — subí el archivo primero, después asociálo acá.</summary>
        public Guid FileId { get; set; }
    }

    public sealed class UpdateTenantRequest
    {
        public string Name { get; set; } = "";
        public string? NotificationEmail { get; set; }
    }

    public sealed class SetTeamUserActiveRequest
    {
        public bool Enabled { get; set; }
    }

    public sealed class ResetTeamUserPasswordRequest
    {
        public string NewPassword { get; set; } = "";
    }

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers(CancellationToken ct)
    {
        var users = await admin.ListUsersAsync(TenantId(), ct);
        return Ok(users);
    }

    /// <summary>Alta de un vendedor/gerente/dueño dentro del propio equipo — exclusivo del Dueño.</summary>
    [HttpPost("users")]
    [Authorize(Roles = Roles.Dueno)]
    public async Task<IActionResult> CreateUser([FromBody] CreateTeamUserRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
            return Problem(title: "Correo y contraseña requeridos", statusCode: StatusCodes.Status400BadRequest);

        var user = await admin.CreateTeamUserAsync(TenantId(), body.Email.Trim(), body.Password, body.Role, ct);
        return CreatedAtAction(nameof(ListUsers), new { version = "1.0" }, user);
    }

    /// <summary>Activa/desactiva a un miembro del propio equipo — exclusivo del Dueño.</summary>
    [HttpPut("users/{userId:guid}/active")]
    [Authorize(Roles = Roles.Dueno)]
    public async Task<IActionResult> SetUserActive(Guid userId, [FromBody] SetTeamUserActiveRequest body, CancellationToken ct)
    {
        var result = await admin.SetTeamUserActiveAsync(TenantId(), userId, body.Enabled, ct);
        return result switch
        {
            null => NotFound(),
            false => Problem(title: "No se pudo actualizar el acceso", statusCode: StatusCodes.Status409Conflict),
            true => NoContent()
        };
    }

    /// <summary>Restablece la contraseña de un miembro del propio equipo — exclusivo del Dueño.</summary>
    [HttpPut("users/{userId:guid}/password")]
    [Authorize(Roles = Roles.Dueno)]
    public async Task<IActionResult> ResetUserPassword(Guid userId, [FromBody] ResetTeamUserPasswordRequest body, CancellationToken ct)
    {
        var result = await admin.ResetTeamUserPasswordAsync(TenantId(), userId, body.NewPassword, ct);
        return result switch
        {
            null => NotFound(),
            false => Problem(title: "No se pudo restablecer la contraseña", statusCode: StatusCodes.Status400BadRequest),
            true => NoContent()
        };
    }

    [HttpGet("tenant")]
    public async Task<IActionResult> GetTenant(CancellationToken ct)
    {
        var tenant = await admin.GetTenantAsync(TenantId(), ct);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    /// <summary>Nombre y correo de notificaciones del propio tenant — exclusivo del Dueño.</summary>
    [HttpPut("tenant")]
    [Authorize(Roles = Roles.Dueno)]
    public async Task<IActionResult> UpdateTenant([FromBody] UpdateTenantRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
            return Problem(title: "Nombre requerido", statusCode: StatusCodes.Status400BadRequest);

        var tenant = await admin.UpdateTenantAsync(TenantId(), body.Name, body.NotificationEmail, ct);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    /// <summary>Asocia un archivo ya subido con POST /v1/files como logo del tenant — exclusivo del Dueño.</summary>
    [HttpPut("tenant/logo")]
    [Authorize(Roles = Roles.Dueno)]
    public async Task<IActionResult> SetLogo([FromBody] SetTenantLogoRequest body, CancellationToken ct)
    {
        var tenant = await admin.SetTenantLogoAsync(TenantId(), body.FileId, ct);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    [HttpGet("clientes")]
    public async Task<IActionResult> Clientes(
        [FromQuery(Name = "updated_since")] DateTimeOffset? updatedSince,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var result = await sync.PullClientesAsync(TenantId(), updatedSince, cursor, limit, ct);
        return Ok(result);
    }

    [HttpGet("productos")]
    public async Task<IActionResult> Productos(
        [FromQuery(Name = "updated_since")] DateTimeOffset? updatedSince,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var result = await sync.PullProductosAsync(TenantId(), updatedSince, cursor, limit, ct);
        // El Gerente ve reportes sin costos ni ganancia — solo el Dueño ve esos números.
        if (!User.IsInRole(Roles.Dueno))
        {
            foreach (var p in result.Items)
            {
                p.PrecioCosto = 0;
                p.PrecioMinimo = null;
            }
        }
        return Ok(result);
    }

    [HttpGet("ventas")]
    public async Task<IActionResult> Ventas(
        [FromQuery(Name = "created_since")] DateTimeOffset? createdSince,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        // Dueño y Gerente ven todo el tenant con atribución (de quién es cada venta).
        var result = await sync.PullVentasAsync(TenantId(), null, createdSince, cursor, limit, ct);
        return Ok(result);
    }

    [HttpGet("cobros")]
    public async Task<IActionResult> Cobros(
        [FromQuery(Name = "created_since")] DateTimeOffset? createdSince,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var result = await sync.PullCobrosAsync(TenantId(), createdSince, cursor, limit, ct);
        return Ok(result);
    }

    [HttpGet("unidades")]
    public async Task<IActionResult> Unidades(
        [FromQuery(Name = "updated_since")] DateTimeOffset? updatedSince,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var result = await sync.PullUnidadesAsync(TenantId(), updatedSince, cursor, limit, ct);
        return Ok(result);
    }

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
    public async Task<IActionResult> ComprasProveedor(
        [FromQuery(Name = "created_since")] DateTimeOffset? createdSince,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var result = await sync.PullComprasProveedorAsync(TenantId(), createdSince, cursor, limit, ct);
        return Ok(result);
    }

    [HttpGet("pagos-proveedor")]
    public async Task<IActionResult> PagosProveedor(
        [FromQuery(Name = "created_since")] DateTimeOffset? createdSince,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var result = await sync.PullPagosProveedorAsync(TenantId(), createdSince, cursor, limit, ct);
        return Ok(result);
    }

    /// <summary>Corrige manualmente el stock de un producto propio — exclusivo del Dueño.</summary>
    [HttpPut("productos/{productoId:guid}/stock")]
    [Authorize(Roles = Roles.Dueno)]
    public async Task<IActionResult> AjustarStock(Guid productoId, [FromBody] AdjustProductoStockRequest body, CancellationToken ct)
    {
        var producto = await admin.AdjustProductoStockAsync(TenantId(), productoId, body.Stock, ct);
        return producto is null ? NotFound() : Ok(producto);
    }

    /// <summary>Corrige manualmente deuda_inicial/saldo_a_favor de un cliente propio — exclusivo del Dueño.</summary>
    [HttpPut("clientes/{clienteId:guid}/saldo")]
    [Authorize(Roles = Roles.Dueno)]
    public async Task<IActionResult> AjustarSaldo(Guid clienteId, [FromBody] AdjustClienteSaldoRequest body, CancellationToken ct)
    {
        var cliente = await admin.AdjustClienteSaldoAsync(TenantId(), clienteId, body.DeudaInicial, body.SaldoAFavor, ct);
        return cliente is null ? NotFound() : Ok(cliente);
    }

    /// <summary>Reversa (total o parcialmente) una venta del propio tenant — exclusivo del Dueño.</summary>
    [HttpPost("ventas/{ventaId:guid}/reverso")]
    [Authorize(Roles = Roles.Dueno)]
    public async Task<IActionResult> ReversarVenta(Guid ventaId, [FromBody] ReversarVentaRequest body, CancellationToken ct)
    {
        try
        {
            var reverso = await admin.ReversarVentaAsync(TenantId(), ventaId, UserId(), null, body, ct);
            return Ok(reverso);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
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
