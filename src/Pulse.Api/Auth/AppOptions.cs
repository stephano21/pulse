namespace Pulse.Api.Auth;

public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>URL pública de la API (sin barra final). Se usa en enlaces de confirmación de correo.</summary>
    public string PublicBaseUrl { get; set; } = "";

    /// <summary>Tenant por defecto para usuarios nuevos (mismo GUID que el seed).</summary>
    public Guid DefaultTenantId { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000001");
}
