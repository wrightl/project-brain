using System.Reflection;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ProjectBrain.Api.Webhooks;
using ProjectBrain.Domain;

namespace ProjectBrain.Api.Tests;

public class WebhookIdempotencyTests
{
    [Fact]
    public void DistributedWebhookIdempotencyService_ShouldOnlyReportProcessedAfterMarking()
    {
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var service = new DistributedWebhookIdempotencyService(cache);

        service.HasProcessed("auth0", "evt_123").Should().BeFalse();

        service.MarkProcessed("auth0", "evt_123", TimeSpan.FromMinutes(5));

        service.HasProcessed("auth0", "evt_123").Should().BeTrue();
    }

    [Fact]
    public async Task Auth0Webhook_ShouldNotMarkEventProcessed_WhenHandlerFails()
    {
        const string eventId = "evt_failed_create";
        var context = CreateWebhookContext(
            """
            {
              "id": "evt_failed_create",
              "type": "user.created",
              "data": {
                "object": {
                  "user_id": "auth0|new-user",
                  "email": "new-user@example.com",
                  "name": "New User",
                  "email_verified": true
                }
              }
            }
            """);

        var userService = new Mock<IUserService>();
        userService.Setup(s => s.GetById("auth0|new-user"))
            .ReturnsAsync((BaseUserDto?)null);
        userService.Setup(s => s.Create(It.IsAny<BaseUserDto>()))
            .ThrowsAsync(new InvalidOperationException("Database unavailable"));

        var idempotencyService = new Mock<IWebhookIdempotencyService>();
        idempotencyService.Setup(s => s.HasProcessed("auth0", eventId)).Returns(false);

        var services = CreateAuth0Services(context, userService.Object);

        var result = await InvokeAuth0Webhook(services, idempotencyService.Object);

        result.Should().NotBeNull();
        idempotencyService.Verify(
            s => s.MarkProcessed(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()),
            Times.Never);
    }

    [Fact]
    public async Task Auth0Webhook_ShouldMarkEventProcessed_AfterSuccessfulHandling()
    {
        const string eventId = "evt_created";
        var context = CreateWebhookContext(
            """
            {
              "id": "evt_created",
              "type": "user.created",
              "data": {
                "object": {
                  "user_id": "auth0|created-user",
                  "email": "created-user@example.com",
                  "name": "Created User",
                  "email_verified": true
                }
              }
            }
            """);

        var userService = new Mock<IUserService>();
        userService.Setup(s => s.GetById("auth0|created-user"))
            .ReturnsAsync((BaseUserDto?)null);
        userService.Setup(s => s.Create(It.IsAny<BaseUserDto>()))
            .ReturnsAsync((BaseUserDto user) => user);

        var emailService = new Mock<IEmailService>();
        emailService.Setup(s => s.SendEmailAsync(
                "created-user@example.com",
                null,
                "welcome email",
                null,
                null,
                null,
                It.IsAny<Dictionary<string, object>>(),
                null,
                null))
            .Returns(Task.CompletedTask);

        var idempotencyService = new Mock<IWebhookIdempotencyService>();
        idempotencyService.Setup(s => s.HasProcessed("auth0", eventId)).Returns(false);

        var services = CreateAuth0Services(context, userService.Object, emailService.Object);

        var result = await InvokeAuth0Webhook(services, idempotencyService.Object);

        result.Should().NotBeNull();
        idempotencyService.Verify(
            s => s.MarkProcessed("auth0", eventId, It.Is<TimeSpan>(ttl => ttl == TimeSpan.FromDays(7))),
            Times.Once);
    }

    private static DefaultHttpContext CreateWebhookContext(string body)
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.Headers.Authorization = "Bearer webhook-secret";
        return context;
    }

    private static Auth0WebhookServices CreateAuth0Services(
        HttpContext context,
        IUserService userService,
        IEmailService? emailService = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth0:WebhookToken"] = "webhook-secret",
            })
            .Build();

        var searchIndexService = new Mock<ISearchIndexService>();
        var storage = new Storage(
            configuration,
            new Azure.Storage.Blobs.BlobServiceClient("UseDevelopmentStorage=true"),
            Mock.Of<ILogger<Storage>>(),
            searchIndexService.Object);

        return new Auth0WebhookServices(
            Mock.Of<ILogger<Auth0WebhookServices>>(),
            userService,
            emailService ?? Mock.Of<IEmailService>(),
            context,
            configuration,
            storage,
            Mock.Of<IUserErasureService>());
    }

    private static async Task<IResult> InvokeAuth0Webhook(
        Auth0WebhookServices services,
        IWebhookIdempotencyService idempotencyService)
    {
        var method = typeof(Auth0WebhookEndpoints)
            .GetMethod("HandleAuth0Webhook", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        var task = (Task<IResult>)method!.Invoke(null, new object[] { services, idempotencyService })!;
        return await task;
    }
}
