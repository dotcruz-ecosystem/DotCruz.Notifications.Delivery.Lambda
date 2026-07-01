using DotCruz.Notifications.Delivery.Lambda.Interfaces;
using Mediator;
using Microsoft.Extensions.Logging;

namespace DotCruz.Notifications.Delivery.Lambda.UseCases.ProcessNotification;

public class ProcessNotificationCommandHandler : IRequestHandler<ProcessNotificationCommand>
{
    private readonly IEnumerable<INotificationSenderStrategy> _senders;
    private readonly INotificationClient _notificationClient;
    private readonly ILogger<ProcessNotificationCommandHandler> _logger;

    public ProcessNotificationCommandHandler(
        IEnumerable<INotificationSenderStrategy> senders,
        INotificationClient notificationClient,
        ILogger<ProcessNotificationCommandHandler> logger)
    {
        _senders = senders;
        _notificationClient = notificationClient;
        _logger = logger;
    }

    public async ValueTask<Unit> Handle(ProcessNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = request.Payload;

        _logger.LogInformation("Processing delivery usecase for notification {NotificationId}", notification.NotificationId);

        bool success = false;
        string? errorMessage = null;

        try
        {
            var sender = _senders.FirstOrDefault(s => s.HandledType.Equals(notification.Type, StringComparison.OrdinalIgnoreCase))
                ?? throw new NotSupportedException($"Notification type '{notification.Type}' is not supported.");
         
            await sender.SendAsync(notification, cancellationToken);
            success = true;
            _logger.LogInformation("Successfully delivered notification {NotificationId} using strategy {StrategyType}", notification.NotificationId, sender.HandledType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver notification {NotificationId}: {ErrorMessage}", notification.NotificationId, ex.Message);
            errorMessage = ex.Message;
        }

        try
        {
            await _notificationClient.UpdateStatusAsync(notification.NotificationId, success, errorMessage, cancellationToken);
            _logger.LogInformation("Callback successfully completed for notification {NotificationId}", notification.NotificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send status callback to VPS for notification {NotificationId}", notification.NotificationId);
        }

        return Unit.Value;
    }
}
