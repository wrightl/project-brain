namespace ProjectBrain.Domain.Dtos;

/// <summary>
/// Result of a push notification send operation
/// </summary>
public class PushNotificationSendResult
{
    /// <summary>
    /// Whether the send operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of tokens that failed with invalid/unregistered errors
    /// </summary>
    public List<string> InvalidTokens { get; set; } = new();

    /// <summary>
    /// List of tokens that failed with other errors
    /// </summary>
    public List<string> FailedTokens { get; set; } = new();

    /// <summary>
    /// Message ID for successful sends (single token)
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Number of successful sends (for multicast)
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed sends (for multicast)
    /// </summary>
    public int FailureCount { get; set; }
}

