using Microsoft.Extensions.Logging;
using ProjectBrain.Domain.Dtos;

/// <summary>
/// No-op implementation of IPushNotificationService used when push notifications are disabled.
/// Does not send any FCM messages; all methods return success so callers are unaffected.
/// </summary>
public class NoOpPushNotificationService : IPushNotificationService
{
    public NoOpPushNotificationService(ILogger<NoOpPushNotificationService> logger)
    {
        logger.LogInformation("Push notifications are disabled; using no-op implementation.");
    }

    public Task<PushNotificationSendResult> SendNotificationAsync(
        string deviceToken,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PushNotificationSendResult
        {
            Success = true,
            MessageId = "noop"
        });
    }

    public Task<PushNotificationSendResult> SendNotificationToMultipleAsync(
        List<string> deviceTokens,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PushNotificationSendResult
        {
            Success = true,
            SuccessCount = deviceTokens?.Count ?? 0,
            FailureCount = 0
        });
    }

    public Task<PushNotificationSendResult> SendNotificationToTopicAsync(
        string topic,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PushNotificationSendResult
        {
            Success = true,
            MessageId = "noop"
        });
    }

    public Task SendDataOnlyToUserAsync(
        string userId,
        IReadOnlyDictionary<string, string> data,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
