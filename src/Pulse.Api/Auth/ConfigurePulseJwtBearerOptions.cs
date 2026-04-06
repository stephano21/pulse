using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Pulse.Api.Auth;

/// <summary>
/// Alinea la validación JWT con <see cref="JwtOptions"/> (la misma fuente que <see cref="TokenService"/>).
/// </summary>
internal sealed class ConfigurePulseJwtBearerOptions(
    IOptions<JwtOptions> jwtOptions,
    IWebHostEnvironment environment) : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(JwtBearerOptions options) =>
        Configure(JwtBearerDefaults.AuthenticationScheme, options);

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
            return;

        var jwt = jwtOptions.Value;
        options.IncludeErrorDetails = environment.IsDevelopment() || jwt.IncludeErrorDetails;
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = c =>
            {
                var log = c.HttpContext.RequestServices.GetService<ILoggerFactory>()
                    ?.CreateLogger("Microsoft.AspNetCore.Authentication.JwtBearer");
                log?.LogWarning(c.Exception, "JWT rechazado: {Message}", c.Exception.Message);
                return Task.CompletedTask;
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    }
}
