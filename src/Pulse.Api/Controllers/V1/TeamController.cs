using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pulse.Application.Abstractions;

namespace Pulse.Api.Controllers.V1;

/// <summary>
/// Gestión del propio equipo y tenant (del que llama). A diferencia de /v1/admin/*, cualquier
/// usuario autenticado puede usarlo para SU tenant — no requiere SuperAdmin. Pensado para que
/// el dueño de un negocio agregue a sus empleados y configure su negocio.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/team")]
[Authorize]
public sealed class TeamController(IAdminService admin) : ControllerBase
{
    public sealed class CreateTeamUserRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public sealed class SetTenantLogoRequest
    {
        /// <summary>Id devuelto por POST /v1/files — subí el archivo primero, después asociálo acá.</summary>
        public Guid FileId { get; set; }
    }

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers(CancellationToken ct)
    {
        var users = await admin.ListUsersAsync(TenantId(), ct);
        return Ok(users);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateTeamUserRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
            return Problem(title: "Correo y contraseña requeridos", statusCode: StatusCodes.Status400BadRequest);

        var user = await admin.CreateTeamUserAsync(TenantId(), body.Email.Trim(), body.Password, ct);
        return CreatedAtAction(nameof(ListUsers), new { version = "1.0" }, user);
    }

    [HttpGet("tenant")]
    public async Task<IActionResult> GetTenant(CancellationToken ct)
    {
        var tenant = await admin.GetTenantAsync(TenantId(), ct);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    /// <summary>Asocia un archivo ya subido con POST /v1/files como logo del tenant.</summary>
    [HttpPut("tenant/logo")]
    public async Task<IActionResult> SetLogo([FromBody] SetTenantLogoRequest body, CancellationToken ct)
    {
        var tenant = await admin.SetTenantLogoAsync(TenantId(), body.FileId, ct);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    private Guid TenantId()
    {
        var v = User.FindFirst("tenant_id")?.Value;
        if (v == null || !Guid.TryParse(v, out var g))
            throw new InvalidOperationException("Falta claim tenant_id.");
        return g;
    }
}
