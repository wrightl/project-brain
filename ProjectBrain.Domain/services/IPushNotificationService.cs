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

    /// <summary>
    /// Sends a data-only FCM message to all active device tokens for the user (no notification title/body).
    /// Used for silent sync triggers (e.g. goals_updated) so the app can refresh in background.
    /// </summary>
    /// <param name="userId">The user ID to resolve device tokens for</param>
    /// <param name="data">Data payload (e.g. type: "goals_updated")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendDataOnlyToUserAsync(
        string userId,
        IReadOnlyDictionary<string, string> data,
        CancellationToken cancellationToken = default);
}

