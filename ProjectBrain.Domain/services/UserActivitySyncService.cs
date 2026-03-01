namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Background service that syncs Redis activity data from the database when requested
/// (on demand or when users are active). Runs only when triggered to avoid unnecessary DB usage.
/// </summary>
public class UserActivitySyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserActivitySyncTrigger _trigger;
    private readonly ILogger<UserActivitySyncService> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    public UserActivitySyncService(
        IServiceProvider serviceProvider,
        IUserActivitySyncTrigger trigger,
        ILogger<UserActivitySyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _trigger = trigger;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(PollInterval, stoppingToken);

            if (!_trigger.GetAndClearNextSyncIfDue())
                continue;

            try
            {
                await SyncActivityDataAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user activity sync");
            }
        }
    }

    private async Task SyncActivityDataAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            // Get all active users from database (those with recent activity)
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var activeUsers = await context.Users
                .Where(u => u.LastActivityAt != null && u.LastActivityAt >= oneHourAgo)
                .Select(u => new { u.Id, u.LastActivityAt })
                .ToListAsync(cancellationToken);

            // Verify each user's activity is in Redis, and if not, ensure it's there
            // This helps recover from Redis failures
            foreach (var user in activeUsers)
            {
                if (user.LastActivityAt == null)
                    continue;

                var cacheKey = $"user:activity:{user.Id}";
                var cachedValue = await cache.GetStringAsync(cacheKey, cancellationToken);

                // If not in cache but should be (based on DB), add it back
                if (string.IsNullOrEmpty(cachedValue) && user.LastActivityAt >= oneHourAgo)
                {
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                    };
                    var timestamp = user.LastActivityAt.Value.ToBinary().ToString();
                    await cache.SetStringAsync(cacheKey, timestamp, cacheOptions, cancellationToken);
                    
                    _logger.LogDebug("Restored user {UserId} activity in Redis cache", user.Id);
                }
            }

            _logger.LogDebug("User activity sync completed. Processed {Count} active users", activeUsers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync user activity data");
            // Don't throw - allow service to continue running
        }
    }
}

