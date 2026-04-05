namespace Pulse.Api.Configuration;

/// <summary>
/// Permite definir la clave de firma HMAC con la variable <c>JWTKey</c> (además de <c>Jwt__SigningKey</c>).
/// </summary>
internal static class JwtEnvironmentBootstrap
{
    public static void Apply()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Jwt__SigningKey")))
            return;

        var key = Environment.GetEnvironmentVariable("JWTKey")
            ?? Environment.GetEnvironmentVariable("JWT_KEY");

        if (!string.IsNullOrWhiteSpace(key))
            Environment.SetEnvironmentVariable("Jwt__SigningKey", key.Trim());
    }
}
