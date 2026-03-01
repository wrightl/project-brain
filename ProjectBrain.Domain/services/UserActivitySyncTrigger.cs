namespace ProjectBrain.Domain;

/// <summary>
/// Triggers user activity sync to run on demand or when user activity is recorded.
/// Sync is debounced so a burst of activity results in one sync shortly after the last request.
/// </summary>
public interface IUserActivitySyncTrigger
{
    /// <summary>
    /// Request that a sync run. Sync will be scheduled for the debounce window from now;
    /// repeated calls within the window reset the schedule so one sync runs after activity settles.
    /// </summary>
    void RequestSync();

    /// <summary>
    /// Returns true if a sync is due (scheduled time has passed) and clears the schedule.
    /// Used by the background service to decide when to run sync.
    /// </summary>
    bool GetAndClearNextSyncIfDue();
}

/// <summary>
/// Singleton implementation of <see cref="IUserActivitySyncTrigger"/> with a configurable debounce window.
/// </summary>
public sealed class UserActivitySyncTrigger : IUserActivitySyncTrigger
{
    private readonly object _lock = new();
    private DateTime? _nextSyncAt;
    private readonly TimeSpan _debounceWindow = TimeSpan.FromMinutes(2);

    public void RequestSync()
    {
        lock (_lock)
        {
            _nextSyncAt = DateTime.UtcNow.Add(_debounceWindow);
        }
    }

    public bool GetAndClearNextSyncIfDue()
    {
        lock (_lock)
        {
            if (_nextSyncAt.HasValue && DateTime.UtcNow >= _nextSyncAt.Value)
            {
                _nextSyncAt = null;
                return true;
            }

            return false;
        }
    }
}
