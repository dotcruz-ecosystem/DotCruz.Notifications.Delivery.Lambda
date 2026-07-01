using System.Text.Json.Serialization;
using Amazon.Lambda.SQSEvents;
using DotCruz.Notifications.Delivery.Lambda.Models;

namespace DotCruz.Notifications.Delivery.Lambda.Serialization;

[JsonSerializable(typeof(SQSEvent))]
[JsonSerializable(typeof(NotificationPayload))]
[JsonSerializable(typeof(NotificationType))]
[JsonSerializable(typeof(UpdateStatusRequest))]
[JsonSerializable(typeof(SmtpCredentials))]
public partial class LambdaJsonSerializerContext : JsonSerializerContext
{
}
