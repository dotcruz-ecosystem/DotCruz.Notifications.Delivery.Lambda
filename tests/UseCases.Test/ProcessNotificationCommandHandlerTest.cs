using CommonTestUtilities.Clients;
using CommonTestUtilities.InlineData;
using CommonTestUtilities.Models;
using CommonTestUtilities.Strategies;
using DotCruz.Notifications.Delivery.Lambda.Interfaces;
using DotCruz.Notifications.Delivery.Lambda.Models;
using DotCruz.Notifications.Delivery.Lambda.UseCases.ProcessNotification;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace UseCases.Test;

public class ProcessNotificationCommandHandlerTest
{
    private static ProcessNotificationCommandHandler CreateSut(
        IEnumerable<INotificationSenderStrategy> senders,
        INotificationClient notificationClient,
        ILogger<ProcessNotificationCommandHandler> logger)
    {
        return new ProcessNotificationCommandHandler(senders, notificationClient, logger);
    }

    [Theory]
    [ClassData(typeof(NotificationTypeInlineDataTest))]
    public async Task Handle_Should_Execute_Correct_Strategy_And_Send_Success_Callback(string notificationType)
    {
        // Arrange
        var payload = new NotificationPayloadBuilder()
            .WithType(notificationType)
            .Build();

        var command = new ProcessNotificationCommand(payload);

        var senderBuilder = new NotificationSenderStrategyBuilder(notificationType)
            .SetupSend();
        var senderMock = senderBuilder.GetMock();

        var otherType = notificationType == "Email" ? "Sms" : "Email";
        var otherSenderBuilder = new NotificationSenderStrategyBuilder(otherType)
            .SetupSend();
        var otherSenderMock = otherSenderBuilder.GetMock();

        var clientBuilder = new NotificationClientBuilder().SetupUpdateStatus();
        var clientMock = clientBuilder.GetMock();

        var loggerMock = new Mock<ILogger<ProcessNotificationCommandHandler>>();

        var sut = CreateSut(
            new[] { senderBuilder.Build(), otherSenderBuilder.Build() },
            clientBuilder.Build(),
            loggerMock.Object
        );

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        senderMock.Verify(s => s.SendAsync(payload, It.IsAny<CancellationToken>()), Times.Once);
        otherSenderMock.Verify(s => s.SendAsync(It.IsAny<NotificationPayload>(), It.IsAny<CancellationToken>()), Times.Never);

        clientMock.Verify(c => c.UpdateStatusAsync(
            payload.NotificationId,
            payload.TenantId,
            true,
            null,
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Theory]
    [ClassData(typeof(NotificationTypeInlineDataTest))]
    public async Task Handle_Should_Send_Failure_Callback_When_Strategy_Throws_Exception(string notificationType)
    {
        var payload = new NotificationPayloadBuilder()
            .WithType(notificationType)
            .Build();

        var command = new ProcessNotificationCommand(payload);
        var expectedException = new Exception(Guid.NewGuid().ToString());

        var senderBuilder = new NotificationSenderStrategyBuilder(notificationType)
            .SetupSendThrows(expectedException);
        var senderMock = senderBuilder.GetMock();

        var clientBuilder = new NotificationClientBuilder().SetupUpdateStatus();
        var clientMock = clientBuilder.GetMock();

        var loggerMock = new Mock<ILogger<ProcessNotificationCommandHandler>>();

        var sut = CreateSut(
            new[] { senderBuilder.Build() },
            clientBuilder.Build(),
            loggerMock.Object
        );

        await sut.Handle(command, CancellationToken.None);

        senderMock.Verify(s => s.SendAsync(payload, It.IsAny<CancellationToken>()), Times.Once);
        clientMock.Verify(c => c.UpdateStatusAsync(
            payload.NotificationId,
            payload.TenantId,
            false,
            expectedException.Message,
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Send_Failure_Callback_When_Type_Is_Unsupported()
    {
        // Arrange
        var randomType = Guid.NewGuid().ToString();
        var payload = new NotificationPayloadBuilder()
            .WithType(randomType)
            .Build();

        var command = new ProcessNotificationCommand(payload);
        var senders = new List<INotificationSenderStrategy>();

        var clientBuilder = new NotificationClientBuilder().SetupUpdateStatus();
        var clientMock = clientBuilder.GetMock();

        var loggerMock = new Mock<ILogger<ProcessNotificationCommandHandler>>();

        var sut = CreateSut(
            senders,
            clientBuilder.Build(),
            loggerMock.Object
        );

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        clientMock.Verify(c => c.UpdateStatusAsync(
            payload.NotificationId,
            payload.TenantId,
            false,
            It.Is<string>(s => s.Contains($"Notification type '{randomType}' is not supported.")),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Theory]
    [ClassData(typeof(NotificationTypeInlineDataTest))]
    public async Task Handle_Should_Not_Throw_Exception_When_Callback_Fails(string notificationType)
    {
        var payload = new NotificationPayloadBuilder()
            .WithType(notificationType)
            .Build();

        var command = new ProcessNotificationCommand(payload);

        var senderBuilder = new NotificationSenderStrategyBuilder(notificationType)
            .SetupSend();

        var clientBuilder = new NotificationClientBuilder()
            .SetupUpdateStatusThrows(new Exception(Guid.NewGuid().ToString()));

        var loggerMock = new Mock<ILogger<ProcessNotificationCommandHandler>>();

        var sut = CreateSut(
            new[] { senderBuilder.Build() },
            clientBuilder.Build(),
            loggerMock.Object
        );

        Func<Task> act = async () => await sut.Handle(command, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
