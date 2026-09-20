using Microsoft.Extensions.Logging;
using Pulse.Application.Abstractions;

namespace Pulse.Infrastructure.Email;

public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        string? replyTo = null,
        string? fromName = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Correo (solo log): To={To} Subject={Subject} ReplyTo={ReplyTo} FromName={FromName} Body={Body}",
            to,
            subject,
            replyTo,
            fromName,
            htmlBody.Length > 500 ? htmlBody[..500] + "…" : htmlBody);
        return Task.CompletedTask;
    }
}
