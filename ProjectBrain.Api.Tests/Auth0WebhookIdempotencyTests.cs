using System.Reflection;
using System.Text;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.AI.Embedding;
using ProjectBrain.Api.Webhooks;
using ProjectBrain.Domain;

namespace ProjectBrain.Api.Tests;

public class Auth0WebhookIdempotencyTests
{
    private const string WebhookToken = "expected-token";
    private const string UserId = "auth0|retry-user";

    [Fact]
    public async Task HandleAuth0Webhook_ShouldRetrySameEvent_WhenPreviousProcessingFailed()
    {
        var userService = new Mock<IUserService>();
        userService.SetupSequence(s => s.GetById(UserId))
            .ReturnsAsync((BaseUserDto?)null)
            .ReturnsAsync((BaseUserDto?)null);
        userService.SetupSequence(s => s.Create(It.IsAny<BaseUserDto>()))
            .ThrowsAsync(new InvalidOperationException("transient database failure"))
            .ReturnsAsync(new BaseUserDto
            {
                Id = UserId,
                Email = "retry@example.com",
                FullName = "Retry User"
            });

        var emailService = new Mock<IEmailService>();
        emailService
            .Setup(s => s.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<Dictionary<string, object>?>(),
                It.IsAny<List<string>?>(),
                It.IsAny<List<string>?>()))
            .Returns(Task.CompletedTask);

        var idempotencyService = new WebhookIdempotencyService(new MemoryCache(new MemoryCacheOptions()));
        var payload = BuildUserCreatedPayload("evt-retry");

        var firstResult = await InvokeWebhook(payload, userService.Object, emailService.Object, idempotencyService);
        var retryResult = await InvokeWebhook(payload, userService.Object, emailService.Object, idempotencyService);
        var duplicateResult = await InvokeWebhook(payload, userService.Object, emailService.Object, idempotencyService);

        Assert.NotNull(firstResult);
        Assert.NotNull(retryResult);
        Assert.NotNull(duplicateResult);
        userService.Verify(s => s.Create(It.Is<BaseUserDto>(u => u.Id == UserId)), Times.Exactly(2));
        emailService.Verify(s => s.SendEmailAsync(
            "retry@example.com",
            It.IsAny<string?>(),
            "welcome email",
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, object>?>(),
            It.IsAny<List<string>?>(),
            It.IsAny<List<string>?>()), Times.Once);
    }

    private static async Task<IResult> InvokeWebhook(
        string payload,
        IUserService userService,
        IEmailService emailService,
        IWebhookIdempotencyService idempotencyService)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {WebhookToken}";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth0:WebhookToken"] = WebhookToken
            })
            .Build();

        var storage = new Storage(
            configuration,
            new BlobServiceClient("UseDevelopmentStorage=true"),
            Mock.Of<ILogger<Storage>>(),
            Mock.Of<ISearchIndexService>());

        var services = new Auth0WebhookServices(
            Mock.Of<ILogger<Auth0WebhookServices>>(),
            userService,
            emailService,
            context,
            configuration,
            storage);

        var method = typeof(Auth0WebhookEndpoints)
            .GetMethod("HandleAuth0Webhook", BindingFlags.NonPublic | BindingFlags.Static);

        var task = (Task<IResult>)method!.Invoke(null, [services, idempotencyService])!;
        return await task;
    }

    private static string BuildUserCreatedPayload(string eventId) =>
        $$"""
        {
          "id": "{{eventId}}",
          "type": "user.created",
          "data": {
            "object": {
              "user_id": "{{UserId}}",
              "email": "retry@example.com",
              "name": "Retry User",
              "email_verified": true,
              "identities": [
                {
                  "connection": "Username-Password-Authentication"
                }
              ]
            }
          }
        }
        """;
}
