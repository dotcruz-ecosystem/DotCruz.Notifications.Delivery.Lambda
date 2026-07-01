using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using DotCruz.Notifications.Delivery.Lambda.Serialization;
using DotCruz.Notifications.Delivery.Lambda.UseCases.ProcessNotification;
using Mediator;
using System.Text.Json;
using DotCruz.Notifications.Delivery.Lambda.Models;

namespace DotCruz.Notifications.Delivery.Lambda;

public class FunctionHandlerService
{
    private readonly IMediator _mediator;

    public FunctionHandlerService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        foreach (var message in evnt.Records)
        {
            context.Logger.LogInformation($"SQS Event Triggered: processing message {message.MessageId}");

            NotificationPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize(message.Body, LambdaJsonSerializerContext.Default.NotificationPayload);
                if (payload == null)
                {
                    context.Logger.LogError($"Failed to deserialize message {message.MessageId} body.");
                    continue;
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Failed to deserialize message {message.MessageId} body. Error: {ex.Message}");
                continue;
            }

            var command = new ProcessNotificationCommand(payload);

            await _mediator.Send(command);
        }
    }
}
