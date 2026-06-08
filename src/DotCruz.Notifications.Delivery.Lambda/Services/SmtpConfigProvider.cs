using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using DotCruz.Notifications.Delivery.Lambda.Interfaces;
using DotCruz.Notifications.Delivery.Lambda.Models;
using DotCruz.Notifications.Delivery.Lambda.Serialization;
using System.Collections.Concurrent;
using System.Text.Json;

namespace DotCruz.Notifications.Delivery.Lambda.Services;

public class SmtpConfigProvider : ISmtpConfigProvider
{
    private readonly IAmazonSimpleSystemsManagement _ssmClient;
    private readonly ConcurrentDictionary<Guid, (SmtpCredentials Credentials, DateTimeOffset Expiration)> _cache = new();
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public SmtpConfigProvider(IAmazonSimpleSystemsManagement ssmClient)
    {
        _ssmClient = ssmClient;
    }

    public async Task<SmtpCredentials?> GetSmtpCredentialsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (_cache.TryGetValue(tenantId, out var cached) && cached.Expiration > now)
            return cached.Credentials;

        try
        {
            var parameterName = string.Format(Environment.GetEnvironmentVariable("PARAMETER_PATH")!, tenantId);
            var request = new GetParameterRequest
            {
                Name = parameterName,
                WithDecryption = true
            };

            var response = await _ssmClient.GetParameterAsync(request, cancellationToken);
            if (response?.Parameter?.Value == null)
            {
                return null;
            }

            var credentials = JsonSerializer.Deserialize(response.Parameter.Value, LambdaJsonSerializerContext.Default.SmtpCredentials);
            if (credentials != null)
            {
                _cache[tenantId] = (credentials, now.Add(_cacheDuration));
            }

            return credentials;
        }
        catch (ParameterNotFoundException)
        {
            return null;
        }
    }
}
