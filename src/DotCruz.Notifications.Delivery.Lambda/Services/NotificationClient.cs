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

    public async Task UpdateStatusAsync(Guid notificationId, Guid? tenantId, bool success, string? errorMessage, CancellationToken cancellationToken = default)
    {
        var statusModel = new UpdateStatusRequest { Success = success, ErrorMessage = errorMessage };

        var path = $"api/Notification/{notificationId}/status";

        using var request = new HttpRequestMessage(HttpMethod.Patch, path)
        {
            Content = JsonContent.Create(statusModel, LambdaJsonSerializerContext.Default.UpdateStatusRequest)
        };

        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
            request.Headers.Add("X-Tenant-ID", tenantId.Value.ToString());

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
