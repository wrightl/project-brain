namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class UserActivityService : IUserActivityService
{
    private readonly IDistributedCache _cache;
    private readonly AppDbContext _context;
    private readonly ILogger<UserActivityService> _logger;
    private const int ActivityWindowHours = 1;
    private const int CacheExpirationSeconds = 3600; // 1 hour
    private const string CacheKeyPrefix = "user:activity:";
    private const string CacheKeySet = "user:activity:set";
    private const string ActiveUsersSetKey = "user:activity:active:set";

    // Debouncing: track last DB update time per user to avoid excessive writes
    private static readonly Dictionary<string, DateTime> _lastDbUpdate = new();
    private static readonly object _dbUpdateLock = new();
    private const int DbUpdateDebounceMinutes = 1; // Update DB max once per minute per user

    public UserActivityService(
        IDistributedCache cache,
        AppDbContext context,
        ILogger<UserActivityService> logger)
    {
        _cache = cache;
        _context = context;
        _logger = logger;
    }

    public async Task RecordUserActivityAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return;

        var now = DateTime.UtcNow;
        var cacheKey = $"{CacheKeyPrefix}{userId}";
        var timestamp = now.ToBinary().ToString();

        try
        {
            // Update Redis cache with expiration
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CacheExpirationSeconds)
            };

            // Store timestamp as value for the user's activity key
            await _cache.SetStringAsync(cacheKey, timestamp, cacheOptions);

            // Also store structured data for efficient queries
            var userActivityData = JsonSerializer.Serialize(new { UserId = userId, Timestamp = now });
            await _cache.SetStringAsync($"{CacheKeySet}:{userId}", userActivityData, cacheOptions);

            // Update the active users set (maintain a JSON array of active user IDs with timestamps)
            await UpdateActiveUsersSetAsync(userId, now, cacheOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update Redis cache for user {UserId}", userId);
            // Continue to database update even if Redis fails
        }

        // Update database with debouncing
        await UpdateDatabaseActivityAsync(userId, now);
    }

    private async Task UpdateActiveUsersSetAsync(string userId, DateTime timestamp, DistributedCacheEntryOptions cacheOptions)
    {
        try
        {
            // Read current active users set
            var setData = await _cache.GetStringAsync(ActiveUsersSetKey);
            var activeUsers = new Dictionary<string, DateTime>();

            if (!string.IsNullOrEmpty(setData))
            {
                try
                {
                    var existingData = JsonSerializer.Deserialize<Dictionary<string, string>>(setData);
                    if (existingData != null)
                    {
                        foreach (var kvp in existingData)
                        {
                            if (DateTime.TryParse(kvp.Value, out var dt))
                            {
                                activeUsers[kvp.Key] = dt;
                            }
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize active users set, resetting");
                    activeUsers = new Dictionary<string, DateTime>();
                }
            }

            // Add/update current user
            activeUsers[userId] = timestamp;

            // Remove expired entries (older than cache expiration)
            var cutoffTime = DateTime.UtcNow.AddSeconds(-CacheExpirationSeconds);
            var expiredKeys = activeUsers
                .Where(kvp => kvp.Value < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                activeUsers.Remove(key);
            }

            // Store updated set
            var serializedData = JsonSerializer.Serialize(
                activeUsers.ToDictionary(k => k.Key, v => v.Value.ToString("O")));
            await _cache.SetStringAsync(ActiveUsersSetKey, serializedData, cacheOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update active users set for user {UserId}", userId);
            // Non-critical, continue execution
        }
    }

    private async Task UpdateDatabaseActivityAsync(string userId, DateTime timestamp)
    {
        // Debounce: only update DB if last update was more than 1 minute ago
        bool shouldUpdate;
        lock (_dbUpdateLock)
        {
            if (!_lastDbUpdate.TryGetValue(userId, out var lastUpdate) ||
                (timestamp - lastUpdate).TotalMinutes >= DbUpdateDebounceMinutes)
            {
                _lastDbUpdate[userId] = timestamp;
                shouldUpdate = true;
            }
            else
            {
                shouldUpdate = false;
            }
        }

        if (!shouldUpdate)
            return;

        try
        {
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE Users SET LastActivityAt = {0} WHERE Id = {1}",
                timestamp, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update database LastActivityAt for user {UserId}", userId);
        }
    }

    public async Task<int> GetActiveUsersCountAsync()
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-ActivityWindowHours);

        try
        {
            // Try Redis first (fast path)
            var count = await GetActiveUsersCountFromCacheAsync(cutoffTime);
            if (count.HasValue)
                return count.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get active users count from Redis, falling back to database");
        }

        // Fallback to database
        return await GetActiveUsersCountFromDatabaseAsync(cutoffTime);
    }

    private async Task<int?> GetActiveUsersCountFromCacheAsync(DateTime cutoffTime)
    {
        try
        {
            // Get active users set from Redis
            var setData = await _cache.GetStringAsync(ActiveUsersSetKey);
            if (string.IsNullOrEmpty(setData))
                return null;

            var activeUsers = JsonSerializer.Deserialize<Dictionary<string, string>>(setData);
            if (activeUsers == null)
                return null;

            // Count users with timestamps >= cutoffTime
            var count = 0;
            foreach (var kvp in activeUsers)
            {
                if (DateTime.TryParse(kvp.Value, out var timestamp) && timestamp >= cutoffTime)
                {
                    count++;
                }
            }

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get active users count from cache");
            return null;
        }
    }

    private async Task<int> GetActiveUsersCountFromDatabaseAsync(DateTime cutoffTime)
    {
        return await _context.Users
            .Where(u => u.LastActivityAt != null && u.LastActivityAt >= cutoffTime)
            .CountAsync();
    }

    public async Task<List<string>> GetActiveUserIdsAsync()
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-ActivityWindowHours);

        try
        {
            // Try Redis first
            var userIds = await GetActiveUserIdsFromCacheAsync(cutoffTime);
            if (userIds != null)
                return userIds;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get active user IDs from Redis, falling back to database");
        }

        // Fallback to database
        return await GetActiveUserIdsFromDatabaseAsync(cutoffTime);
    }

    private async Task<List<string>?> GetActiveUserIdsFromCacheAsync(DateTime cutoffTime)
    {
        try
        {
            // Get active users set from Redis
            var setData = await _cache.GetStringAsync(ActiveUsersSetKey);
            if (string.IsNullOrEmpty(setData))
                return null;

            var activeUsers = JsonSerializer.Deserialize<Dictionary<string, string>>(setData);
            if (activeUsers == null)
                return null;

            // Filter users with timestamps >= cutoffTime
            var activeUserIds = new List<string>();
            foreach (var kvp in activeUsers)
            {
                if (DateTime.TryParse(kvp.Value, out var timestamp) && timestamp >= cutoffTime)
                {
                    activeUserIds.Add(kvp.Key);
                }
            }

            return activeUserIds;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get active user IDs from cache");
            return null;
        }
    }

    private async Task<List<string>> GetActiveUserIdsFromDatabaseAsync(DateTime cutoffTime)
    {
        return await _context.Users
            .Where(u => u.LastActivityAt != null && u.LastActivityAt >= cutoffTime)
            .Select(u => u.Id)
            .ToListAsync();
    }

    public async Task<bool> IsUserActiveAsync(string userId, int activityWindowMinutes = 60)
    {
        if (string.IsNullOrEmpty(userId))
            return false;

        var cutoffTime = DateTime.UtcNow.AddMinutes(-activityWindowMinutes);
        var cacheKey = $"{CacheKeyPrefix}{userId}";

        try
        {
            // Try Redis first (fast path)
            var cachedTimestamp = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedTimestamp))
            {
                // Parse the timestamp
                if (long.TryParse(cachedTimestamp, out var binaryTimestamp))
                {
                    var activityTime = DateTime.FromBinary(binaryTimestamp);
                    // Check if activity is within the requested window
                    if (activityTime >= cutoffTime)
                    {
                        return true;
                    }
                }
            }

            // If Redis doesn't have the data or it's expired, check the structured cache
            var setKey = $"{CacheKeySet}:{userId}";
            var userActivityData = await _cache.GetStringAsync(setKey);
            if (!string.IsNullOrEmpty(userActivityData))
            {
                try
                {
                    var activity = JsonSerializer.Deserialize<JsonElement>(userActivityData);
                    if (activity.TryGetProperty("Timestamp", out var timestampProp))
                    {
                        if (DateTime.TryParse(timestampProp.GetString(), out var timestamp) && timestamp >= cutoffTime)
                        {
                            return true;
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse user activity data for user {UserId}", userId);
                }
            }

            // If activity window is longer than cache expiration, or Redis doesn't have data, fallback to database
            // This handles cases where the cache has expired but the user might still be active
            if (activityWindowMinutes * 60 > CacheExpirationSeconds)
            {
                var user = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.LastActivityAt })
                    .FirstOrDefaultAsync();

                if (user == null)
                    return false;

                return user.LastActivityAt != null && user.LastActivityAt >= cutoffTime;
            }

            // If cache doesn't have the data and window is within cache expiration, user is not active
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check if user {UserId} is active from Redis, falling back to database", userId);

            // Fallback to database on error
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.LastActivityAt })
                    .FirstOrDefaultAsync();

                if (user == null)
                    return false;

                return user.LastActivityAt != null && user.LastActivityAt >= cutoffTime;
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Failed to check if user {UserId} is active from database", userId);
                return false;
            }
        }
    }
}

