namespace Pulse.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Servidor SMTP (vacío = no SMTP; se usa solo log).</summary>
    public string Host { get; set; } = "";

    public int Port { get; set; } = 587;

    public string? UserName { get; set; }
    public string? Password { get; set; }

    /// <summary>Dirección remitente (obligatoria si usas SMTP).</summary>
    public string FromAddress { get; set; } = "";

    public string FromName { get; set; } = "Pulse";

    /// <summary>None (p. ej. Mailpit local), StartTls (587 típico), SslOnConnect (465).</summary>
    public string SslMode { get; set; } = "StartTls";

    public bool IsSmtpConfigured =>
        !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
}
