using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotCruz.Notifications.Delivery.Lambda.Interfaces;

public interface INotificationClient
{
    Task UpdateStatusAsync(Guid notificationId, Guid? tenantId, bool success, string? errorMessage, CancellationToken cancellationToken = default);
}
