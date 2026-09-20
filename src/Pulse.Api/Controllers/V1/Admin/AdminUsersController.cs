using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pulse.Application.Abstractions;
using Pulse.Infrastructure.Identity;

namespace Pulse.Api.Controllers.V1.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/admin/users")]
[Authorize(Roles = Roles.SuperAdmin)]
public sealed class AdminUsersController(IAdminService admin) : ControllerBase
{
    public sealed class SetSuperAdminRequest
    {
        public bool Enabled { get; set; }
    }

    public sealed class SetEmailConfirmedRequest
    {
        public bool Enabled { get; set; }
    }

    public sealed class SetActiveRequest
    {
        public bool Enabled { get; set; }
    }

    public sealed class MoveTenantRequest
    {
        public Guid TenantId { get; set; }
    }

    public sealed class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = "";
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery(Name = "tenant_id")] Guid? tenantId, CancellationToken ct)
    {
        var users = await admin.ListUsersAsync(tenantId, ct);
        return Ok(users);
    }

    [HttpPut("{userId:guid}/super-admin")]
    public async Task<IActionResult> SetSuperAdmin(Guid userId, [FromBody] SetSuperAdminRequest body, CancellationToken ct)
    {
        var ok = await admin.SetSuperAdminAsync(userId, body.Enabled, ct);
        if (!ok)
            return Problem(
                title: "No se pudo actualizar el rol",
                detail: "El usuario no existe o esta acción dejaría el sistema sin ningún SuperAdmin.",
                statusCode: StatusCodes.Status409Conflict);

        return NoContent();
    }

    [HttpPut("{userId:guid}/email-confirmed")]
    public async Task<IActionResult> SetEmailConfirmed(Guid userId, [FromBody] SetEmailConfirmedRequest body, CancellationToken ct)
    {
        var ok = await admin.SetEmailConfirmedAsync(userId, body.Enabled, ct);
        if (!ok)
            return NotFound();

        return NoContent();
    }

    [HttpPut("{userId:guid}/active")]
    public async Task<IActionResult> SetActive(Guid userId, [FromBody] SetActiveRequest body, CancellationToken ct)
    {
        var ok = await admin.SetUserActiveAsync(userId, body.Enabled, ct);
        if (!ok)
            return Problem(
                title: "No se pudo actualizar el acceso",
                detail: "El usuario no existe o esta acción dejaría el sistema sin ningún SuperAdmin activo.",
                statusCode: StatusCodes.Status409Conflict);

        return NoContent();
    }

    [HttpPut("{userId:guid}/tenant")]
    public async Task<IActionResult> MoveTenant(Guid userId, [FromBody] MoveTenantRequest body, CancellationToken ct)
    {
        var user = await admin.MoveUserToTenantAsync(userId, body.TenantId, ct);
        if (user is null)
            return Problem(title: "No se pudo mover el usuario", detail: "El usuario o el tenant destino no existen.", statusCode: StatusCodes.Status404NotFound);

        return Ok(user);
    }

    [HttpPut("{userId:guid}/password")]
    public async Task<IActionResult> ResetPassword(Guid userId, [FromBody] ResetPasswordRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.NewPassword))
            return Problem(title: "Contraseña requerida", statusCode: StatusCodes.Status400BadRequest);

        var ok = await admin.ResetPasswordAsync(userId, body.NewPassword, ct);
        if (!ok)
            return NotFound();

        return NoContent();
    }
}
