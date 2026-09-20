using Microsoft.AspNetCore.Identity;

namespace Pulse.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid? TenantId { get; set; }

    /// <summary>Origen principal del alta: <see cref="AuthProviders.Local"/> o <see cref="AuthProviders.Google"/>.</summary>
    public string AuthProvider { get; set; } = AuthProviders.Local;

    /// <summary>Identificador estable de Google (<c>sub</c> del token); trazabilidad sin consultar AspNetUserLogins.</summary>
    public string? GoogleSubject { get; set; }

    /// <summary>URL externa (Google la da al hacer login) — no es nuestro storage.</summary>
    public string? ProfilePictureUrl { get; set; }

    /// <summary>
    /// Referencia a <see cref="Pulse.Domain.StoredFile"/> subido por el propio usuario
    /// (POST /v1/files, luego asociado con PUT /v1/profile/photo). Si está seteado, tiene
    /// prioridad sobre <see cref="ProfilePictureUrl"/>.
    /// </summary>
    public Guid? AvatarFileId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }
}
