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
}
