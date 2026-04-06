using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Pulse.Api.Auth;

/// <summary>
/// Emite el JWT de sesión de Pulse (HS256). No confundir con el id_token de Google: ese solo se usa en <c>POST /v1/auth/google</c>.
/// </summary>
public sealed class TokenService(IOptions<JwtOptions> jwtOptions)
{
    public string CreateAccessToken(Guid tenantId, string subject, string? email = null, IEnumerable<string>? scopes = null)
    {
        var opt = jwtOptions.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject),
            new("tenant_id", tenantId.ToString())
        };
        if (email is not null)
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, email));
        foreach (var s in scopes ?? ["sync:read", "sync:write"])
            claims.Add(new Claim("scope", s));

        var token = new JwtSecurityToken(
            issuer: opt.Issuer,
            audience: opt.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddHours(12),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
