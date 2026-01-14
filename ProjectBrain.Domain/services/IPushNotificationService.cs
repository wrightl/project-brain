using ProjectBrain.Domain.Dtos;

/// <summary>
/// Service interface for sending push notifications via Firebase Cloud Messaging
/// </summary>
public interface IPushNotificationService
{
    /// <summary>
    /// Sends a push notification to a single device token
    /// </summary>
    /// <param name="deviceToken">The FCM device token</param>
    /// <param name="title">Notification title</param>
    /// <param name="body">Notification body</param>
    /// <param name="data">Optional data payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the send operation</returns>
    Task<PushNotificationSendResult> SendNotificationAsync(
        string deviceToken,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a push notification to multiple device tokens
    /// </summary>
    /// <param name="deviceTokens">List of FCM device tokens</param>
    /// <param name="title">Notification title</param>
    /// <param name="body">Notification body</param>
    /// <param name="data">Optional data payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the send operation</returns>
    Task<PushNotificationSendResult> SendNotificationToMultipleAsync(
        List<string> deviceTokens,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a push notification to a topic
    /// </summary>
    /// <param name="topic">The FCM topic</param>
    /// <param name="title">Notification title</param>
    /// <param name="body">Notification body</param>
    /// <param name="data">Optional data payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the send operation</returns>
    Task<PushNotificationSendResult> SendNotificationToTopicAsync(
        string topic,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);
}

