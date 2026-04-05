namespace Pulse.Api.Configuration;

/// <summary>
/// Traduce variables de entorno planas al esquema <c>Email__*</c> que lee la API.
/// Ejecutar antes de <see cref="Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder"/>.
/// </summary>
internal static class EmailEnvironmentBootstrap
{
    public static void Apply()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Email__Host")))
            return;

        var address = FirstNonEmpty(
            "Email__FromAddress",
            "EMAIL_FROM",
            "EMAIL_ADDRESS",
            "Email",
            "EMAIL");

        var host = FirstNonEmpty(
            "Email__Host",
            "EMAIL_HOST",
            "SMTP_HOST",
            "Host");

        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(host))
            return;

        var userName = FirstNonEmpty(
            "Email__UserName",
            "EMAIL_USER",
            "SMTP_USER",
            "Email",
            "EMAIL",
            "EMAIL_ADDRESS");

        var port = FirstNonEmpty(
            "Email__Port",
            "EMAIL_SMTP_PORT",
            "SMTP_PORT",
            "Port");

        var password = FirstNonEmptyPreserveInner(
            "Email__Password",
            "EMAIL_PASSWORD",
            "SMTP_PASSWORD",
            "Password");

        Environment.SetEnvironmentVariable("Email__FromAddress", address.Trim());
        Environment.SetEnvironmentVariable("Email__UserName", (userName ?? address).Trim());
        Environment.SetEnvironmentVariable("Email__Host", host.Trim());

        if (!string.IsNullOrWhiteSpace(port))
            Environment.SetEnvironmentVariable("Email__Port", port.Trim());

        if (!string.IsNullOrWhiteSpace(password))
            Environment.SetEnvironmentVariable("Email__Password", password.Trim());

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Email__SslMode")))
            Environment.SetEnvironmentVariable("Email__SslMode", "StartTls");
    }

    private static string? FirstNonEmpty(params string[] keys)
    {
        foreach (var k in keys)
        {
            var v = Environment.GetEnvironmentVariable(k);
            if (!string.IsNullOrWhiteSpace(v))
                return v;
        }

        return null;
    }

    /// <summary>Recorta solo espacios extremos; las contraseñas de aplicación de Gmail conservan espacios internos.</summary>
    private static string? FirstNonEmptyPreserveInner(params string[] keys)
    {
        foreach (var k in keys)
        {
            var v = Environment.GetEnvironmentVariable(k);
            if (v is null)
                continue;
            var t = v.Trim();
            if (t.Length > 0)
                return t;
        }

        return null;
    }
}
