using System.IdentityModel.Tokens.Jwt;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Pulse.Application.Abstractions;
using Pulse.Infrastructure.Identity;

namespace Pulse.Api.Controllers.V1;

/// <summary>El perfil del usuario que llama: su propia cuenta, no la de otro (para eso está /v1/admin o /v1/team).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/profile")]
[Authorize]
public sealed class ProfileController(
    UserManager<ApplicationUser> userManager,
    IAdminService admin,
    IFileRegistry files,
    IFileStorageService storage) : ControllerBase
{
    public sealed class SetProfilePhotoRequest
    {
        /// <summary>Id devuelto por POST /v1/files — subí la foto primero, después asociála acá.</summary>
        public Guid FileId { get; set; }
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var user = await CurrentUserAsync();
        if (user is null)
            return Unauthorized();

        var tenant = user.TenantId.HasValue ? await admin.GetTenantAsync(user.TenantId.Value, ct) : null;

        string? avatarUrl = user.ProfilePictureUrl; // respaldo: foto de Google si nunca subió una propia
        if (user.AvatarFileId.HasValue)
        {
            var file = await files.GetAsync(user.AvatarFileId.Value, ct);
            if (file is not null && storage.IsConfigured)
                avatarUrl = storage.GetPresignedUrl(file.Key);
        }

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            profile_picture_url = avatarUrl,
            tenant_id = tenant?.Id,
            tenant_name = tenant?.Name,
            tenant_logo_url = tenant?.LogoUrl
        });
    }

    /// <summary>Asocia un archivo ya subido con POST /v1/files como foto de perfil.</summary>
    [HttpPut("photo")]
    public async Task<IActionResult> SetPhoto([FromBody] SetProfilePhotoRequest body, CancellationToken ct)
    {
        var user = await CurrentUserAsync();
        if (user is null)
            return Unauthorized();

        var file = await files.GetAsync(body.FileId, ct);
        if (file is null || file.UploadedByUserId != user.Id)
            return Problem(title: "Archivo inválido", detail: "No existe o no lo subiste vos.", statusCode: StatusCodes.Status400BadRequest);

        user.AvatarFileId = body.FileId;
        await userManager.UpdateAsync(user);

        return Ok(new { profile_picture_url = storage.IsConfigured ? storage.GetPresignedUrl(file.Key) : null });
    }

    /// <summary>
    /// UserManager.GetUserAsync busca ClaimTypes.NameIdentifier, pero TokenService solo emite el
    /// claim estándar "sub" (JsonWebTokenHandler, el handler por defecto desde .NET 8, no remapea
    /// claims automáticamente salvo que se configure MapInboundClaims — acá no está configurado).
    /// Por eso resolvemos el id a mano, igual que ya se hace con tenant_id en el resto de la API.
    /// </summary>
    private Task<ApplicationUser?> CurrentUserAsync()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return sub is null ? Task.FromResult<ApplicationUser?>(null) : userManager.FindByIdAsync(sub);
    }
}
