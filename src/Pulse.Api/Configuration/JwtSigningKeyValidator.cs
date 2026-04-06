using System.Text;
using Microsoft.Extensions.Configuration;
using Pulse.Api.Auth;

namespace Pulse.Api.Configuration;

/// <summary>
/// HS256 exige una clave simétrica de al menos 256 bits (32 bytes en UTF-8).
/// </summary>
internal static class JwtSigningKeyValidator
{
    public const int MinimumKeyByteLength = 32;

    public static void Validate(IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var keyBytes = Encoding.UTF8.GetBytes(jwt.SigningKey ?? "");
        if (keyBytes.Length >= MinimumKeyByteLength)
            return;

        throw new InvalidOperationException(
            $"La clave JWT mide {keyBytes.Length} bytes en UTF-8; HS256 requiere al menos {MinimumKeyByteLength} bytes (256 bits). " +
            "Amplía JWTKey, Jwt__SigningKey o Jwt:SigningKey (p. ej. 32+ caracteres aleatorios o un secreto generado).");
    }
}
