using Bogus;
using DotCruz.Notifications.Delivery.Lambda.Models;

namespace CommonTestUtilities.Models;

public class NotificationPayloadBuilder
{
    private Guid _notificationId;
    private string _type;
    private string _recipient;
    private string _title;
    private string _body;
    private Guid? _tenantId;

    public NotificationPayloadBuilder()
    {
        var faker = new Faker();
        _notificationId = faker.Random.Guid();
        _type = faker.PickRandom(new[] { "Email", "Sms", "Push" });
        _recipient = faker.Person.Email;
        _title = faker.Lorem.Sentence();
        _body = faker.Lorem.Paragraph();
        _tenantId = null;
    }

    public NotificationPayloadBuilder WithNotificationId(Guid id)
    {
        _notificationId = id;
        return this;
    }

    public NotificationPayloadBuilder WithType(string type)
    {
        _type = type;
        return this;
    }

    public NotificationPayloadBuilder WithRecipient(string recipient)
    {
        _recipient = recipient;
        return this;
    }

    public NotificationPayloadBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public NotificationPayloadBuilder WithBody(string body)
    {
        _body = body;
        return this;
    }

    public NotificationPayloadBuilder WithTenantId(Guid? tenantId)
    {
        _tenantId = tenantId;
        return this;
    }

    public NotificationPayload Build()
    {
        return new NotificationPayload
        {
            NotificationId = _notificationId,
            Type = _type,
            Recipient = _recipient,
            Title = _title,
            Body = _body,
            TenantId = _tenantId
        };
    }
}
