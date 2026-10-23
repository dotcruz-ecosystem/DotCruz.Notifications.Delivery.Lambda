using DotCruz.Notifications.Delivery.Lambda.Interfaces;
using DotCruz.Notifications.Delivery.Lambda.Models;
using MailKit.Net.Smtp;
using MimeKit;

namespace DotCruz.Notifications.Delivery.Lambda.Services.Senders;

public class EmailSenderStrategy : INotificationSenderStrategy
{
    public string HandledType => "Email";

    public async Task SendAsync(NotificationPayload payload, CancellationToken cancellationToken)
    {
        var host = Environment.GetEnvironmentVariable("SMTP_HOST")
            ?? throw new InvalidOperationException("SMTP_HOST env variable is not set.");

        var portEnv = Environment.GetEnvironmentVariable("SMTP_PORT");
        var port = int.TryParse(portEnv, out var p) ? p : 587;

        var username = Environment.GetEnvironmentVariable("SMTP_USERNAME")
            ?? throw new InvalidOperationException("SMTP_USERNAME env variable is not set.");

        var password = Environment.GetEnvironmentVariable("SMTP_PASSWORD")
            ?? throw new InvalidOperationException("SMTP_PASSWORD env variable is not set.");

        var fromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME")
            ?? "DotCruz Notifications";

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(fromName, username));
        email.To.Add(new MailboxAddress(string.Empty, payload.Recipient));
        email.Subject = payload.Title;

        var bodyBuilder = new BodyBuilder { HtmlBody = payload.Body };
        email.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        client.Timeout = 15000;

        var socketOptions = port switch
        {
            465 => MailKit.Security.SecureSocketOptions.SslOnConnect,
            587 => MailKit.Security.SecureSocketOptions.StartTls,
            _ => MailKit.Security.SecureSocketOptions.Auto
        };

        await client.ConnectAsync(host, port, socketOptions, cancellationToken);
        await client.AuthenticateAsync(username, password, cancellationToken);
        await client.SendAsync(email, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
