using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pulse.Application.Abstractions;

namespace Pulse.Api.Controllers.V1;

/// <summary>
/// Gestión del propio equipo (usuarios del tenant del que llama). A diferencia de
/// /v1/admin/*, cualquier usuario autenticado puede usarlo para SU tenant — no requiere
/// SuperAdmin. Pensado para que el dueño de un negocio agregue a sus empleados.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/team/users")]
[Authorize]
public sealed class TeamController(IAdminService admin) : ControllerBase
{
    public sealed class CreateTeamUserRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var users = await admin.ListUsersAsync(TenantId(), ct);
        return Ok(users);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeamUserRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
            return Problem(title: "Correo y contraseña requeridos", statusCode: StatusCodes.Status400BadRequest);

        var user = await admin.CreateTeamUserAsync(TenantId(), body.Email.Trim(), body.Password, ct);
        return CreatedAtAction(nameof(List), new { version = "1.0" }, user);
    }

    private Guid TenantId()
    {
        var v = User.FindFirst("tenant_id")?.Value;
        if (v == null || !Guid.TryParse(v, out var g))
            throw new InvalidOperationException("Falta claim tenant_id.");
        return g;
    }
}
