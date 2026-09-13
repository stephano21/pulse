using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pulse.Application.Abstractions;
using Pulse.Infrastructure.Identity;

namespace Pulse.Api.Controllers.V1.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/admin/tenants")]
[Authorize(Roles = Roles.SuperAdmin)]
public sealed class AdminTenantsController(IAdminService admin) : ControllerBase
{
    public sealed class CreateTenantRequest
    {
        public string Name { get; set; } = "";
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var tenants = await admin.ListTenantsAsync(ct);
        return Ok(tenants);
    }

    [HttpGet("{tenantId:guid}")]
    public async Task<IActionResult> Get(Guid tenantId, CancellationToken ct)
    {
        var tenant = await admin.GetTenantAsync(tenantId, ct);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
            return Problem(title: "Nombre requerido", statusCode: StatusCodes.Status400BadRequest);

        var tenant = await admin.CreateTenantAsync(body.Name, ct);
        return CreatedAtAction(nameof(Get), new { tenantId = tenant.Id, version = "1.0" }, tenant);
    }
}
