using Microsoft.Extensions.Logging;
using Pulse.Application.Abstractions;

namespace Pulse.Infrastructure.Email;

public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Correo (solo log): To={To} Subject={Subject} Body={Body}",
            to,
            subject,
            htmlBody.Length > 500 ? htmlBody[..500] + "…" : htmlBody);
        return Task.CompletedTask;
    }
}
