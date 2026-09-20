namespace Pulse.Application.Abstractions;

public interface IEmailSender
{
    /// <param name="replyTo">Null = sin Reply-To (responden al From genérico de la app).</param>
    /// <param name="fromName">Null = usa Email:FromName del .env. El From (dirección) siempre es
    /// la cuenta autenticada — nunca la del tenant, para no romper SPF/DKIM.</param>
    Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        string? replyTo = null,
        string? fromName = null,
        CancellationToken cancellationToken = default);
}
