using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Pulse.Api.Auth;
using Pulse.Application.Abstractions;
using Pulse.Application.Email;
using Pulse.Infrastructure.Identity;

namespace Pulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/auth")]
public sealed class AuthController(
    TokenService tokens,
    IRefreshTokenService refreshTokens,
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    IAdminService admin,
    IOptions<AppOptions> appOptions,
    IOptions<GoogleAuthOptions> googleOptions,
    IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    private readonly AppOptions _app = appOptions.Value;
    private readonly GoogleAuthOptions _google = googleOptions.Value;
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public sealed class RegisterRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";

        /// <summary>Nombre del negocio: se crea como tenant nuevo, propio de este usuario.</summary>
        public string TenantName { get; set; } = "";
    }

    public sealed class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public sealed class ResendConfirmationRequest
    {
        public string Email { get; set; } = "";
    }

    public sealed class GoogleTokenRequest
    {
        /// <summary>ID token JWT devuelto por Google Sign-In (cliente).</summary>
        public string IdToken { get; set; } = "";
    }

    public sealed class RefreshRequest
    {
        public string RefreshToken { get; set; } = "";
    }

    public sealed class LogoutRequest
    {
        public string RefreshToken { get; set; } = "";
    }

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    /// <summary>Registro con correo y contraseña. Envía enlace de confirmación (en desarrollo se registra en logs).</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_app.PublicBaseUrl))
            return Problem(
                title: "Configuración incompleta",
                detail: "Falta App:PublicBaseUrl para generar el enlace de confirmación.",
                statusCode: StatusCodes.Status503ServiceUnavailable);

        if (string.IsNullOrWhiteSpace(body.TenantName))
            return Problem(title: "Nombre del negocio requerido", statusCode: StatusCodes.Status400BadRequest);

        var email = body.Email.Trim();
        var now = DateTimeOffset.UtcNow;
        var tenant = await admin.CreateTenantAsync(body.TenantName, cancellationToken);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            TenantId = tenant.Id,
            AuthProvider = AuthProviders.Local,
            CreatedAt = now
        };

        var result = await userManager.CreateAsync(user, body.Password);
        if (!result.Succeeded)
            return Problem(
                title: "No se pudo registrar",
                detail: string.Join(" ", result.Errors.Select(e => e.Description)),
                statusCode: StatusCodes.Status400BadRequest);

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var tokenEncoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmUrl =
            $"{_app.PublicBaseUrl.TrimEnd('/')}/v1/auth/confirm-email?user_id={user.Id}&token={tokenEncoded}";

        var html = EmailTemplate.Render(
            heading: "Confirmá tu correo",
            bodyHtml: "<p>Gracias por registrarte en Yapa. Confirmá tu correo para activar tu cuenta y empezar a usarla.</p>",
            ctaText: "Confirmar correo",
            ctaUrl: confirmUrl);
        await emailSender.SendEmailAsync(email, "Confirmá tu correo — Yapa", html, cancellationToken: cancellationToken);

        return Ok(new { message = "Si el correo es válido, recibirás un enlace de confirmación." });
    }

    /// <summary>Confirma el correo a partir del enlace enviado por email.</summary>
    [HttpGet("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromQuery] Guid user_id, [FromQuery] string token)
    {
        var user = await userManager.FindByIdAsync(user_id.ToString());
        if (user is null)
            return Problem(title: "Usuario no encontrado", statusCode: StatusCodes.Status404NotFound);

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch
        {
            return Problem(title: "Token inválido", statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await userManager.ConfirmEmailAsync(user, decoded);
        if (!result.Succeeded)
            return Problem(
                title: "No se pudo confirmar",
                detail: string.Join("; ", result.Errors.Select(e => e.Description)),
                statusCode: StatusCodes.Status400BadRequest);

        return Ok(new { message = "Correo confirmado. Ya puedes iniciar sesión." });
    }

    /// <summary>Inicio de sesión con correo y contraseña (requiere correo confirmado).</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest body)
    {
        var email = body.Email.Trim();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return Unauthorized();

        if (!await userManager.CheckPasswordAsync(user, body.Password))
            return Unauthorized();

        if (!await userManager.IsEmailConfirmedAsync(user))
            return Problem(
                title: "Correo sin confirmar",
                detail: "Confirma tu correo antes de iniciar sesión.",
                statusCode: StatusCodes.Status403Forbidden);

        if (await userManager.IsLockedOutAsync(user))
            return Problem(
                title: "Cuenta desactivada",
                detail: "Esta cuenta fue desactivada. Contacta al administrador.",
                statusCode: StatusCodes.Status403Forbidden);

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        var tenantId = user.TenantId ?? _app.DefaultTenantId;
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokens.CreateAccessToken(tenantId, user.Id.ToString(), user.Email, roles: roles);
        var refresh = await refreshTokens.IssueAsync(tenantId, user.Id, _jwt.RefreshTokenLifetimeDays, ClientIp(), HttpContext.RequestAborted);
        return Ok(new
        {
            access_token = accessToken,
            refresh_token = refresh.RawToken,
            token_type = "Bearer",
            expires_in = 12 * 3600
        });
    }

    /// <summary>Vuelve a enviar el enlace de confirmación.</summary>
    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendConfirmation(
        [FromBody] ResendConfirmationRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_app.PublicBaseUrl))
            return Problem(
                title: "Configuración incompleta",
                detail: "Falta App:PublicBaseUrl.",
                statusCode: StatusCodes.Status503ServiceUnavailable);

        var email = body.Email.Trim();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || await userManager.IsEmailConfirmedAsync(user))
            return Ok(new { message = "Si el correo existe y no está confirmado, recibirás un nuevo enlace." });

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var tokenEncoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmUrl =
            $"{_app.PublicBaseUrl.TrimEnd('/')}/v1/auth/confirm-email?user_id={user.Id}&token={tokenEncoded}";

        var html = EmailTemplate.Render(
            heading: "Confirmá tu correo",
            bodyHtml: "<p>Pediste un nuevo enlace para confirmar tu cuenta de Yapa.</p>",
            ctaText: "Confirmar correo",
            ctaUrl: confirmUrl);

        string? replyTo = null;
        string? fromName = null;
        if (user.TenantId.HasValue)
        {
            var tenant = await admin.GetTenantAsync(user.TenantId.Value, cancellationToken);
            if (!string.IsNullOrWhiteSpace(tenant?.NotificationEmail))
            {
                replyTo = tenant.NotificationEmail;
                fromName = $"{tenant.Name} (vía Yapa)";
            }
        }
        await emailSender.SendEmailAsync(email, "Confirmá tu correo — Yapa", html, replyTo, fromName, cancellationToken);

        return Ok(new { message = "Si el correo existe y no está confirmado, recibirás un nuevo enlace." });
    }

    /// <summary>Inicio de sesión con Google: envía el id_token del cliente (Sign-In con Google).</summary>
    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<IActionResult> Google([FromBody] GoogleTokenRequest body)
    {
        if (string.IsNullOrWhiteSpace(_google.ClientId))
            return Problem(
                title: "Google no configurado",
                detail: "Define Authentication:Google:ClientId.",
                statusCode: StatusCodes.Status503ServiceUnavailable);

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(body.IdToken, new()
            {
                Audience = [_google.ClientId]
            });
        }
        catch (InvalidJwtException)
        {
            return Problem(title: "Token de Google inválido", statusCode: StatusCodes.Status401Unauthorized);
        }

        if (string.IsNullOrEmpty(payload.Email) || !payload.EmailVerified)
            return Problem(
                title: "Correo no verificado en Google",
                statusCode: StatusCodes.Status403Forbidden);

        var email = payload.Email;
        var pictureUrl = NormalizePictureUrl(payload.Picture);
        var loginInfo = new UserLoginInfo("Google", payload.Subject, "Google");

        var user = await userManager.FindByLoginAsync(loginInfo.LoginProvider, loginInfo.ProviderKey);
        if (user is null)
        {
            user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                var now = DateTimeOffset.UtcNow;
                // Google no pide nombre de negocio en el propio flujo, así que usamos el nombre de
                // perfil (o el usuario del correo como respaldo) como punto de partida; el
                // SuperAdmin puede renombrar el tenant después desde el admin panel.
                var tenantName = !string.IsNullOrWhiteSpace(payload.Name) ? payload.Name! : email.Split('@')[0];
                var tenant = await admin.CreateTenantAsync(tenantName, HttpContext.RequestAborted);
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    TenantId = tenant.Id,
                    AuthProvider = AuthProviders.Google,
                    GoogleSubject = payload.Subject,
                    ProfilePictureUrl = pictureUrl,
                    CreatedAt = now,
                    LastLoginAt = now
                };
                var randomPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) + "Aa1!";
                var create = await userManager.CreateAsync(user, randomPassword);
                if (!create.Succeeded)
                    return Problem(
                        title: "No se pudo crear la cuenta",
                        detail: string.Join(" ", create.Errors.Select(e => e.Description)),
                        statusCode: StatusCodes.Status400BadRequest);
            }

            var addLogin = await userManager.AddLoginAsync(user, loginInfo);
            if (!addLogin.Succeeded)
                return Problem(
                    title: "No se pudo vincular Google",
                    detail: string.Join(" ", addLogin.Errors.Select(e => e.Description)),
                    statusCode: StatusCodes.Status400BadRequest);
        }

        if (await userManager.IsLockedOutAsync(user))
            return Problem(
                title: "Cuenta desactivada",
                detail: "Esta cuenta fue desactivada. Contacta al administrador.",
                statusCode: StatusCodes.Status403Forbidden);

        if (!user.EmailConfirmed)
            user.EmailConfirmed = true;

        user.GoogleSubject = payload.Subject;
        user.LastLoginAt = DateTimeOffset.UtcNow;
        if (pictureUrl is not null)
            user.ProfilePictureUrl = pictureUrl;

        await userManager.UpdateAsync(user);

        var tenantId = user.TenantId ?? _app.DefaultTenantId;
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokens.CreateAccessToken(tenantId, user.Id.ToString(), user.Email, roles: roles);
        var refresh = await refreshTokens.IssueAsync(tenantId, user.Id, _jwt.RefreshTokenLifetimeDays, ClientIp(), HttpContext.RequestAborted);
        return Ok(new
        {
            access_token = accessToken,
            refresh_token = refresh.RawToken,
            token_type = "Bearer",
            expires_in = 12 * 3600
        });
    }

    /// <summary>Rota el refresh token y emite un nuevo access token. El refresh token presentado queda revocado (uso único).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest body, CancellationToken ct)
    {
        var result = await refreshTokens.ValidateAndRotateAsync(body.RefreshToken, _jwt.RefreshTokenLifetimeDays, ClientIp(), ct);
        if (!result.Success || result.TenantId is null || result.UserId is null || result.NewRawToken is null)
            return Unauthorized();

        var user = await userManager.FindByIdAsync(result.UserId.Value.ToString());
        if (user is null)
            return Unauthorized();

        if (await userManager.IsLockedOutAsync(user))
            return Unauthorized();

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokens.CreateAccessToken(result.TenantId.Value, user.Id.ToString(), user.Email, roles: roles);
        return Ok(new
        {
            access_token = accessToken,
            refresh_token = result.NewRawToken,
            token_type = "Bearer",
            expires_in = 12 * 3600
        });
    }

    /// <summary>Revoca el refresh token (cierre de sesión). Idempotente: nunca falla aunque el token ya sea inválido.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest body, CancellationToken ct)
    {
        await refreshTokens.RevokeAsync(body.RefreshToken, ct);
        return NoContent();
    }

    private static string? NormalizePictureUrl(string? picture) =>
        string.IsNullOrWhiteSpace(picture) ? null : picture.Trim();
}
