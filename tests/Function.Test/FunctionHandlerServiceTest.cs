using Amazon.Lambda.SQSEvents;
using CommonTestUtilities.Lambda;
using CommonTestUtilities.MediatR;
using CommonTestUtilities.Models;
using DotCruz.Notifications.Delivery.Lambda;
using DotCruz.Notifications.Delivery.Lambda.Serialization;
using DotCruz.Notifications.Delivery.Lambda.UseCases.ProcessNotification;
using Mediator;
using Moq;
using System.Text.Json;

namespace Function.Test;

public class FunctionHandlerServiceTest
{
    private static FunctionHandlerService CreateSut(IMediator mediator)
    {
        return new FunctionHandlerService(mediator);
    }

    [Fact]
    public async Task FunctionHandler_Should_Dispatch_ProcessNotificationCommand_To_Mediator()
    {
        // Arrange
        var mediatorBuilder = new MediatorBuilder();
        var mediatorMock = mediatorBuilder.GetMock();
        var sut = CreateSut(mediatorBuilder.Build());

        var payload = new NotificationPayloadBuilder().WithTenantId(Guid.NewGuid()).Build();
        var messageBody = JsonSerializer.Serialize(payload, LambdaJsonSerializerContext.Default.NotificationPayload);

        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new() { MessageId = Guid.NewGuid().ToString(), Body = messageBody }
            }
        };

        var contextBuilder = new LambdaContextBuilder();

        // Act
        await sut.FunctionHandler(sqsEvent, contextBuilder.Build());

        // Assert
        mediatorMock.Verify(m => m.Send(
            It.Is<ProcessNotificationCommand>(c => 
                c.Payload.NotificationId == payload.NotificationId &&
                c.Payload.Type == payload.Type &&
                c.Payload.Recipient == payload.Recipient &&
                c.Payload.Title == payload.Title &&
                c.Payload.Body == payload.Body &&
                c.Payload.TenantId == payload.TenantId
            ), 
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task FunctionHandler_Should_Log_Error_When_Payload_Is_Null_And_Not_Call_Mediator()
    {
        // Arrange
        var mediatorBuilder = new MediatorBuilder();
        var mediatorMock = mediatorBuilder.GetMock();
        var sut = CreateSut(mediatorBuilder.Build());

        var messageId = Guid.NewGuid().ToString();
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new() { MessageId = messageId, Body = "invalid-json" }
            }
        };

        var contextBuilder = new LambdaContextBuilder();
        var loggerMock = contextBuilder.GetLoggerMock();

        // Act
        await sut.FunctionHandler(sqsEvent, contextBuilder.Build());

        // Assert
        loggerMock.Verify(l => l.LogError(It.Is<string>(s => s.Contains($"Failed to deserialize message {messageId} body"))), Times.Once);
        mediatorMock.Verify(m => m.Send(It.IsAny<ProcessNotificationCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
