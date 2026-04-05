using Microsoft.AspNetCore.Identity;

namespace Pulse.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid? TenantId { get; set; }

    /// <summary>Origen principal del alta: <see cref="AuthProviders.Local"/> o <see cref="AuthProviders.Google"/>.</summary>
    public string AuthProvider { get; set; } = AuthProviders.Local;

    /// <summary>Identificador estable de Google (<c>sub</c> del token); trazabilidad sin consultar AspNetUserLogins.</summary>
    public string? GoogleSubject { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }
}
