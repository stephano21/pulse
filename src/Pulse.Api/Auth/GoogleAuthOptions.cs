namespace Pulse.Api.Auth;

public sealed class GoogleAuthOptions
{
    public const string SectionName = "Authentication:Google";

    /// <summary>Client ID de OAuth de Google (audience del id_token).</summary>
    public string ClientId { get; set; } = "";
}
