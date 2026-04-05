using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Pulse.Application.Abstractions;

namespace Pulse.Infrastructure.Email;

public sealed class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _opt = options.Value;

    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (!_opt.IsSmtpConfigured)
            throw new InvalidOperationException("SMTP no está configurado (Email:Host / Email:FromAddress).");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_opt.FromName, _opt.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        var socket = ParseSslMode(_opt.SslMode);

        using var client = new SmtpClient();
        client.Timeout = 60_000;

        await client.ConnectAsync(_opt.Host, _opt.Port, socket, cancellationToken).ConfigureAwait(false);

        var smtpUser = string.IsNullOrWhiteSpace(_opt.UserName) ? _opt.FromAddress : _opt.UserName;
        if (!string.IsNullOrWhiteSpace(smtpUser) && _opt.Password is { Length: > 0 })
            await client.AuthenticateAsync(smtpUser, _opt.Password, cancellationToken).ConfigureAwait(false);

        await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Correo enviado por SMTP a {To} asunto {Subject}", to, subject);
    }

    private static SecureSocketOptions ParseSslMode(string? mode) =>
        mode?.Trim().ToUpperInvariant() switch
        {
            "NONE" => SecureSocketOptions.None,
            "SSLONCONNECT" or "SSL" => SecureSocketOptions.SslOnConnect,
            _ => SecureSocketOptions.StartTls
        };
}
