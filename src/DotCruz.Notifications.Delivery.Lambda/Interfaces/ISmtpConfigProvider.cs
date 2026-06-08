using System;
using System.Threading;
using System.Threading.Tasks;
using DotCruz.Notifications.Delivery.Lambda.Models;

namespace DotCruz.Notifications.Delivery.Lambda.Interfaces;

public interface ISmtpConfigProvider
{
    Task<SmtpCredentials?> GetSmtpCredentialsAsync(Guid tenantId, CancellationToken cancellationToken);
}
