using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Amazon.SimpleSystemsManagement;
using DotCruz.Notifications.Delivery.Lambda.Interfaces;
using DotCruz.Notifications.Delivery.Lambda.Serialization;
using DotCruz.Notifications.Delivery.Lambda.Services;
using DotCruz.Notifications.Delivery.Lambda.Services.Senders;
using Microsoft.Extensions.DependencyInjection;

namespace DotCruz.Notifications.Delivery.Lambda;

public class Program
{
    private static async Task Main(string[] args)
    {
        var serviceProvider = ConfigureServices();
        var handler = serviceProvider.GetRequiredService<FunctionHandlerService>();

        using var handlerWrapper = HandlerWrapper.GetHandlerWrapper<SQSEvent>(
            handler.FunctionHandler, 
            new SourceGeneratorLambdaJsonSerializer<LambdaJsonSerializerContext>()
        );
        
        using var bootstrap = new LambdaBootstrap(handlerWrapper);
        await bootstrap.RunAsync();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<HttpClient>();

        services.AddSingleton<IAmazonSimpleSystemsManagement, AmazonSimpleSystemsManagementClient>();
        services.AddSingleton<ISmtpConfigProvider, SmtpConfigProvider>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

        services.AddTransient<INotificationSenderStrategy, EmailSenderStrategy>();
        services.AddTransient<INotificationSenderStrategy, SmsSenderStrategy>();
        services.AddTransient<INotificationSenderStrategy, PushSenderStrategy>();

        services.AddHttpClient<INotificationClient, NotificationClient>(client =>
        {
            var apiUrl = Environment.GetEnvironmentVariable("NOTIFICATIONS_API_URL")
                ?? throw new InvalidOperationException("NOTIFICATIONS_API_URL env variable is not set.");
            client.BaseAddress = new Uri(apiUrl);

            client.DefaultRequestHeaders.Add("X-Api-Key", Environment.GetEnvironmentVariable("NOTIFICATIONS_API_KEY"));
        });

        services.AddTransient<FunctionHandlerService>();

        return services.BuildServiceProvider();
    }
}
