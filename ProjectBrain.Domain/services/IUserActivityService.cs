namespace ProjectBrain.Domain;

public interface IUserActivityService
{
    /// <summary>
    /// Records user activity by updating both Redis cache and database.
    /// </summary>
    Task RecordUserActivityAsync(string userId);

    /// <summary>
    /// Gets the count of active users (active within the last hour) from Redis cache.
    /// Falls back to database if Redis is unavailable.
    /// </summary>
    Task<int> GetActiveUsersCountAsync();

    /// <summary>
    /// Gets the list of active user IDs (active within the last hour) from Redis cache.
    /// Falls back to database if Redis is unavailable.
    /// </summary>
    Task<List<string>> GetActiveUserIdsAsync();

    /// <summary>
    /// Checks if a specific user is active within the specified time window.
    /// </summary>
    /// <param name="userId">The user ID to check</param>
    /// <param name="activityWindowMinutes">The time window in minutes (default: 60)</param>
    Task<bool> IsUserActiveAsync(string userId, int activityWindowMinutes = 60);
}

