namespace Pulse.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "pulse";
    public string Audience { get; set; } = "yapa";

    /// <summary>
    /// Secreto HMAC (mín. ~32 caracteres recomendado). En entorno: <c>Jwt__SigningKey</c> o variable <c>JWTKey</c>.
    /// </summary>
    public string SigningKey { get; set; } = "";
}
