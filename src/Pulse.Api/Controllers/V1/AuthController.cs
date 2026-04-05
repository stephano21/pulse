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
using Pulse.Infrastructure.Identity;

namespace Pulse.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/auth")]
public sealed class AuthController(
    TokenService tokens,
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    IOptions<AppOptions> appOptions,
    IOptions<GoogleAuthOptions> googleOptions,
    IHostEnvironment env) : ControllerBase
{
    private readonly AppOptions _app = appOptions.Value;
    private readonly GoogleAuthOptions _google = googleOptions.Value;

    public sealed class DevTokenRequest
    {
        public Guid TenantId { get; set; }
        public string Subject { get; set; } = "dev-device";
    }

    public sealed class RegisterRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
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

    /// <summary>Solo disponible en entorno Development: emite un JWT de prueba con tenant_id.</summary>
    [HttpPost("token")]
    [AllowAnonymous]
    public IActionResult DevToken([FromBody] DevTokenRequest body)
    {
        if (!env.IsDevelopment())
            return NotFound();

        var token = tokens.CreateAccessToken(body.TenantId, body.Subject);
        return Ok(new { access_token = token, token_type = "Bearer", expires_in = 12 * 3600 });
    }

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

        var email = body.Email.Trim();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            TenantId = _app.DefaultTenantId
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

        var html =
            $"""
             <p>Activa tu cuenta Pulse pulsando el enlace (o cópialo en el navegador):</p>
             <p><a href="{confirmUrl}">Confirmar correo</a></p>
             <p><code>{confirmUrl}</code></p>
             """;
        await emailSender.SendEmailAsync(email, "Confirma tu correo — Pulse", html, cancellationToken);

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

        var tenantId = user.TenantId ?? _app.DefaultTenantId;
        var accessToken = tokens.CreateAccessToken(tenantId, user.Id.ToString(), user.Email);
        return Ok(new { access_token = accessToken, token_type = "Bearer", expires_in = 12 * 3600 });
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

        var html =
            $"""
             <p>Confirma tu cuenta Pulse:</p>
             <p><a href="{confirmUrl}">Confirmar correo</a></p>
             <p><code>{confirmUrl}</code></p>
             """;
        await emailSender.SendEmailAsync(email, "Confirma tu correo — Pulse", html, cancellationToken);

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
        var loginInfo = new UserLoginInfo("Google", payload.Subject, "Google");

        var user = await userManager.FindByLoginAsync(loginInfo.LoginProvider, loginInfo.ProviderKey);
        if (user is null)
        {
            user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    TenantId = _app.DefaultTenantId
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

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
        }

        var tenantId = user.TenantId ?? _app.DefaultTenantId;
        var accessToken = tokens.CreateAccessToken(tenantId, user.Id.ToString(), user.Email);
        return Ok(new { access_token = accessToken, token_type = "Bearer", expires_in = 12 * 3600 });
    }
}
