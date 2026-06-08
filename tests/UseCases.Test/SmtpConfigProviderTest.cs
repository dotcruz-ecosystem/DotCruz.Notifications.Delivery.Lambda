using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using DotCruz.Notifications.Delivery.Lambda.Models;
using DotCruz.Notifications.Delivery.Lambda.Services;
using FluentAssertions;
using Moq;
using System.Text.Json;

namespace UseCases.Test;

public class SmtpConfigProviderTest
{
    private readonly Mock<IAmazonSimpleSystemsManagement> _ssmClientMock;
    private readonly SmtpConfigProvider _provider;

    public SmtpConfigProviderTest()
    {
        Environment.SetEnvironmentVariable("PARAMETER_PATH", "/dotcruz/tenants/{0}/smtp");
        _ssmClientMock = new Mock<IAmazonSimpleSystemsManagement>();
        _provider = new SmtpConfigProvider(_ssmClientMock.Object);
    }

    [Fact]
    public async Task GetSmtpCredentialsAsync_Should_CacheResult_And_NotCallSsmMultipleTimes()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var credentialsJson = JsonSerializer.Serialize(new SmtpCredentials
        {
            Host = "smtp.tenant.com",
            Port = 587,
            Username = "user@tenant.com",
            Password = "password123"
        });

        _ssmClientMock.Setup(x => x.GetParameterAsync(
            It.IsAny<GetParameterRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetParameterResponse
            {
                Parameter = new Parameter { Value = credentialsJson }
            });

        // Act
        var firstResult = await _provider.GetSmtpCredentialsAsync(tenantId, CancellationToken.None);
        var secondResult = await _provider.GetSmtpCredentialsAsync(tenantId, CancellationToken.None);

        // Assert
        firstResult.Should().NotBeNull();
        secondResult.Should().NotBeNull();
        secondResult!.Host.Should().Be(firstResult!.Host);

        _ssmClientMock.Verify(x => x.GetParameterAsync(
            It.Is<GetParameterRequest>(r => r.Name == $"/dotcruz/tenants/{tenantId}/smtp"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSmtpCredentialsAsync_Should_ReturnNull_When_ParameterNotFound()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _ssmClientMock.Setup(x => x.GetParameterAsync(
            It.IsAny<GetParameterRequest>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ParameterNotFoundException("Parameter not found"));

        // Act
        var result = await _provider.GetSmtpCredentialsAsync(tenantId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
