using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Pulse.Application.Abstractions;
using Pulse.Application.Email;
using Pulse.Infrastructure.Identity;

namespace Pulse.Api.Controllers.V1.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/admin/diagnostics")]
[Authorize(Roles = Roles.SuperAdmin)]
public sealed class AdminDiagnosticsController(
    IEmailSender emailSender,
    IAdminService admin,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    public sealed class SendTestEmailRequest
    {
        /// <summary>Vacío = se manda al correo de quien llama.</summary>
        public string? To { get; set; }

        /// <summary>Si se manda, prueba con el Reply-To/nombre configurado en ese tenant.</summary>
        public Guid? TenantId { get; set; }
    }

    [HttpPost("test-email")]
    public async Task<IActionResult> SendTestEmail([FromBody] SendTestEmailRequest body, CancellationToken ct)
    {
        var to = body.To?.Trim();
        if (string.IsNullOrWhiteSpace(to))
        {
            // No confiamos solo en el claim "email" del JWT (puede faltar en tokens viejos u otros
            // casos raros de emisión) — resolvemos el correo real desde la base. Confirmado en
            // producción: GetUserAsync funciona (el pipeline remapea "sub" a ClaimTypes.NameIdentifier).
            var caller = await userManager.GetUserAsync(User);
            to = caller?.Email;
        }

        if (string.IsNullOrWhiteSpace(to))
            return Problem(title: "Falta destinatario", statusCode: StatusCodes.Status400BadRequest);

        string? replyTo = null;
        string? fromName = null;
        if (body.TenantId.HasValue)
        {
            var tenant = await admin.GetTenantAsync(body.TenantId.Value, ct);
            if (tenant is null)
                return Problem(title: "Tenant no encontrado", statusCode: StatusCodes.Status404NotFound);
            if (!string.IsNullOrWhiteSpace(tenant.NotificationEmail))
            {
                replyTo = tenant.NotificationEmail;
                fromName = $"{tenant.Name} (vía Yapa)";
            }
        }

        var html = EmailTemplate.Render(
            heading: "Correo de prueba",
            bodyHtml: $"""
                       <p>Si estás viendo esto, la configuración SMTP de Yapa está funcionando.</p>
                       {(replyTo is not null ? $"<p>Probado con el Reply-To de <strong>{fromName}</strong>: {replyTo}.</p>" : "")}
                       <p>Enviado el {DateTimeOffset.UtcNow:dd/MM/yyyy HH:mm} UTC desde el panel admin.</p>
                       """);

        try
        {
            await emailSender.SendEmailAsync(to, "Correo de prueba — Yapa", html, replyTo, fromName, ct);
        }
        catch (Exception ex)
        {
            // A diferencia del resto de la API, acá SÍ queremos el mensaje crudo del proveedor
            // SMTP (auth, host, TLS, etc.): es justo lo que hace falta para diagnosticar la config.
            return Problem(title: "No se pudo enviar el correo", detail: ex.Message, statusCode: StatusCodes.Status502BadGateway);
        }

        return Ok(new { message = $"Correo de prueba enviado a {to}." });
    }
}
