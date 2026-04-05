namespace Pulse.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "pulse";
    public string Audience { get; set; } = "yapa";
    public string SigningKey { get; set; } = "";
}
