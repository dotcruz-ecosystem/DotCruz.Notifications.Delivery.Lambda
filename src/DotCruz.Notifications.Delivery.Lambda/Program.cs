using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Amazon.SimpleSystemsManagement;
using DotCruz.Notifications.Delivery.Lambda.Interfaces;
using DotCruz.Notifications.Delivery.Lambda.Serialization;
using DotCruz.Notifications.Delivery.Lambda.Services;
using DotCruz.Notifications.Delivery.Lambda.Services.Senders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<HttpClient>();

        services.AddSingleton<IAmazonSimpleSystemsManagement, AmazonSimpleSystemsManagementClient>();
        services.AddSingleton<ISmtpConfigProvider, SmtpConfigProvider>();

        services.AddMediator();

        services.AddTransient<INotificationSenderStrategy, EmailSenderStrategy>();
        services.AddTransient<INotificationSenderStrategy, SmsSenderStrategy>();
        services.AddTransient<INotificationSenderStrategy, PushSenderStrategy>();

        services.AddHttpClient<INotificationClient, NotificationClient>(client =>
        {
            var apiUrl = GetEnvironmentVariable("NOTIFICATIONS_API_URL");
            
            if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException($"The environment variable NOTIFICATIONS_API_URL has an invalid URI format: '{apiUrl}'.");
            }
            
            client.BaseAddress = uri;

            var apiKey = GetEnvironmentVariable("DOT_CRUZ_API_KEY", "NOTIFICATIONS_API_KEY");
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        });

        services.AddTransient<FunctionHandlerService>();

        return services.BuildServiceProvider();
    }

    private static string GetEnvironmentVariable(string name, string? fallbackName = null)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value) && fallbackName != null)
        {
            value = Environment.GetEnvironmentVariable(fallbackName);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            var errorMsg = fallbackName != null 
                ? $"Environment variable '{name}' (or fallback '{fallbackName}') is not set or is empty."
                : $"Environment variable '{name}' is not set or is empty.";
            throw new InvalidOperationException(errorMsg);
        }

        return value.Trim().Trim('"').Trim('\'');
    }
}
