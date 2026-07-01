using System;
using System.Text.Json.Serialization;

namespace DotCruz.Notifications.Delivery.Lambda.Models;

public enum NotificationType
{
    Email = 0,
    Sms = 1,
    Push = 2
}

public class NotificationPayload
{
    private string _type = string.Empty;
    private NotificationType _notificationType;

    public Guid NotificationId { get; set; }
    
    public string Type 
    { 
        get => string.IsNullOrEmpty(_type) ? _notificationType.ToString() : _type; 
        set 
        {
            _type = value;
            if (Enum.TryParse<NotificationType>(value, true, out var result))
            {
                _notificationType = result;
            }
        }
    }

    public NotificationType NotificationType 
    { 
        get => _notificationType; 
        set 
        {
            _notificationType = value;
            if (string.IsNullOrEmpty(_type))
            {
                _type = value.ToString();
            }
        }
    }

    public string Recipient { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
}
