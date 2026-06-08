using System.Text.Json.Serialization;

namespace DotCruz.Notifications.Delivery.Lambda.Models;

public class SmtpCredentials
{
    public string FromName { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
