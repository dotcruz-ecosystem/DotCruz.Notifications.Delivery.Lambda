using DotCruz.Notifications.Delivery.Lambda.Interfaces;
using DotCruz.Notifications.Delivery.Lambda.Models;
using DotCruz.Notifications.Delivery.Lambda.Serialization;
using System.Net.Http.Json;

namespace DotCruz.Notifications.Delivery.Lambda.Services;

public class NotificationClient : INotificationClient
{
    private readonly HttpClient _httpClient;

    public NotificationClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task UpdateStatusAsync(Guid notificationId, bool success, string? errorMessage, CancellationToken cancellationToken = default)
    {
        var statusModel = new UpdateStatusRequest { Success = success, ErrorMessage = errorMessage };

        var path = $"api/Notification/{notificationId}/status";
        var response = await _httpClient.PatchAsJsonAsync(
            path, 
            statusModel, 
            LambdaJsonSerializerContext.Default.UpdateStatusRequest, 
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
    }
}
