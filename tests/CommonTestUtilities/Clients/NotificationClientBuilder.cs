using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using DotCruz.Notifications.Delivery.Lambda.Interfaces;

namespace CommonTestUtilities.Clients;

public class NotificationClientBuilder
{
    private readonly Mock<INotificationClient> _client;

    public NotificationClientBuilder()
    {
        _client = new Mock<INotificationClient>();
    }

    public NotificationClientBuilder SetupUpdateStatus()
    {
        _client.Setup(c => c.UpdateStatusAsync(
            It.IsAny<Guid>(), 
            It.IsAny<Guid?>(), 
            It.IsAny<bool>(), 
            It.IsAny<string?>(), 
            It.IsAny<CancellationToken>()
        )).Returns(Task.CompletedTask);
        return this;
    }

    public NotificationClientBuilder SetupUpdateStatusThrows(Exception exception)
    {
        _client.Setup(c => c.UpdateStatusAsync(
            It.IsAny<Guid>(), 
            It.IsAny<Guid?>(), 
            It.IsAny<bool>(), 
            It.IsAny<string?>(), 
            It.IsAny<CancellationToken>()
        )).ThrowsAsync(exception);
        return this;
    }

    public INotificationClient Build() => _client.Object;
    public Mock<INotificationClient> GetMock() => _client;
}
