using DotCruz.Notifications.Delivery.Lambda.Models;
using Mediator;

namespace DotCruz.Notifications.Delivery.Lambda.UseCases.ProcessNotification;

public record ProcessNotificationCommand(
    NotificationPayload Payload
) : IRequest;
