using System.IdentityModel.Tokens.Jwt;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pulse.Api.Uploads;
using Pulse.Application.Abstractions;

namespace Pulse.Api.Controllers.V1;

/// <summary>
/// Endpoint genérico de subida: sube el archivo al storage, registra sus metadatos (tabla
/// "files", sin relación fija a ningún tipo de entidad) y devuelve un Id. Recién con ese Id se
/// asocia a lo que corresponda (ej. PUT /v1/team/tenant/logo, PUT /v1/profile/photo) — subir y
/// asociar son dos pasos separados a propósito, para no acoplar el storage a cada feature.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/files")]
[Authorize]
public sealed class FilesController(IFileStorageService storage, IFileRegistry registry) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(ImageValidation.MaxSizeBytes)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken ct)
    {
        var error = ImageValidation.Validate(file);
        if (error is not null)
            return Problem(title: error, statusCode: StatusCodes.Status400BadRequest);

        if (!storage.IsConfigured)
            return Problem(title: "El storage de archivos no está configurado", statusCode: StatusCodes.Status503ServiceUnavailable);

        var fileId = Guid.NewGuid();
        var key = $"files/{fileId}{ImageValidation.ExtensionFor(file!)}";

        await using (var stream = file!.OpenReadStream())
            await storage.UploadAsync(key, stream, file.ContentType, ct);

        await registry.RegisterAsync(fileId, TenantId(), UserId(), key, file.ContentType, file.Length, ct);

        return Ok(new { id = fileId, url = storage.GetPresignedUrl(key) });
    }

    [HttpGet("{fileId:guid}")]
    public async Task<IActionResult> Get(Guid fileId, CancellationToken ct)
    {
        var file = await registry.GetAsync(fileId, ct);
        if (file is null)
            return NotFound();
        if (file.TenantId.HasValue && file.TenantId != TenantId())
            return Forbid();

        return Ok(new { id = file.Id, url = storage.GetPresignedUrl(file.Key), content_type = file.ContentType });
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
        var v = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (v == null || !Guid.TryParse(v, out var g))
            throw new InvalidOperationException("Falta claim sub.");
        return g;
    }
}
