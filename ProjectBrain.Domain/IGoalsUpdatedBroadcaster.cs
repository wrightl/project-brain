namespace ProjectBrain.Domain;

/// <summary>
/// Broadcasts goals-updated events to subscribed clients (e.g. SSE streams) per user.
/// </summary>
public interface IGoalsUpdatedBroadcaster
{
    /// <summary>
    /// Subscribe to goals-updated events for a user. The <paramref name="writeEvent"/> callback
    /// is invoked for each event until <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    Task SubscribeAsync(
        string userId,
        Func<GoalsUpdatedEvent, CancellationToken, Task> writeEvent,
        CancellationToken cancellationToken);

    /// <summary>
    /// Notify all subscribers for the given user that goals were updated (fan-out only to that user).
    /// </summary>
    void NotifyGoalsUpdated(string userId, GoalsUpdatedEvent evt);
}
