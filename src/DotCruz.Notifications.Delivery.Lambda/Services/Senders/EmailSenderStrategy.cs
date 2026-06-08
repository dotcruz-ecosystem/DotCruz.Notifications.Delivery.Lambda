using DotCruz.Notifications.Delivery.Lambda.Interfaces;
using DotCruz.Notifications.Delivery.Lambda.Models;
using MailKit.Net.Smtp;
using MimeKit;

namespace DotCruz.Notifications.Delivery.Lambda.Services.Senders;

public class EmailSenderStrategy : INotificationSenderStrategy
{
    private readonly ISmtpConfigProvider _smtpConfigProvider;

    public string HandledType => "Email";

    public EmailSenderStrategy(ISmtpConfigProvider smtpConfigProvider)
    {
        _smtpConfigProvider = smtpConfigProvider;
    }

    public async Task SendAsync(NotificationPayload payload, CancellationToken cancellationToken)
    {
        SmtpCredentials? tenantCredentials = null;

        if (payload.TenantId.HasValue)
            tenantCredentials = await _smtpConfigProvider.GetSmtpCredentialsAsync(payload.TenantId.Value, cancellationToken);

        var host = (!string.IsNullOrEmpty(tenantCredentials?.Host) ? tenantCredentials.Host : null)
            ?? Environment.GetEnvironmentVariable("SMTP_HOST")
            ?? throw new InvalidOperationException("SMTP_HOST env variable is not set.");

        var portVal = tenantCredentials?.Port > 0 ? tenantCredentials.Port : (int?)null;
        if (portVal == null)
        {
            var portEnv = Environment.GetEnvironmentVariable("SMTP_PORT");
            portVal = int.TryParse(portEnv, out var p) ? p : 587;
        }
        var port = portVal.Value;

        var username = (!string.IsNullOrEmpty(tenantCredentials?.Username) ? tenantCredentials.Username : null)
            ?? Environment.GetEnvironmentVariable("SMTP_USERNAME")
            ?? throw new InvalidOperationException("SMTP_USERNAME env variable is not set.");

        var password = (!string.IsNullOrEmpty(tenantCredentials?.Password) ? tenantCredentials.Password : null)
            ?? Environment.GetEnvironmentVariable("SMTP_PASSWORD")
            ?? throw new InvalidOperationException("SMTP_PASSWORD env variable is not set.");

        var fromName = (!string.IsNullOrEmpty(tenantCredentials?.FromName) ? tenantCredentials.FromName : null)
            ?? Environment.GetEnvironmentVariable("SMTP_FROM_NAME")
            ?? "DotCruz Notifications";


        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(fromName, username));
        email.To.Add(new MailboxAddress(string.Empty, payload.Recipient));
        email.Subject = payload.Title;

        var bodyBuilder = new BodyBuilder { HtmlBody = payload.Body };
        email.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, false, cancellationToken);
        await client.AuthenticateAsync(username, password, cancellationToken);
        await client.SendAsync(email, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
