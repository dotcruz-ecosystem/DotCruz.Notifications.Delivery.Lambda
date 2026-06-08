using System;

namespace DotCruz.Notifications.Delivery.Lambda.Models;

public class NotificationPayload
{
    public Guid NotificationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
}
