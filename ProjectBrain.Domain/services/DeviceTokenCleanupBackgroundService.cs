using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Background service that periodically cleans up invalid and stale device tokens
/// </summary>
public class DeviceTokenCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeviceTokenCleanupBackgroundService> _logger;
    private readonly TimeSpan _cleanupInterval;

    public DeviceTokenCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DeviceTokenCleanupBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        var cleanupIntervalHours = configuration.GetValue<int>("PushNotifications:CleanupIntervalHours", 24);
        _cleanupInterval = TimeSpan.FromHours(cleanupIntervalHours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DeviceTokenCleanupBackgroundService started. Cleanup interval: {Interval} hours", _cleanupInterval.TotalHours);

        // Wait a bit before first cleanup to allow application to fully start
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during device token cleanup background task");
            }

            // Wait for the next cleanup interval
            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("DeviceTokenCleanupBackgroundService stopped");
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var cleanupService = scope.ServiceProvider.GetRequiredService<IDeviceTokenCleanupService>();

        try
        {
            _logger.LogInformation("Starting scheduled device token cleanup");

            // Run proactive validation cleanup
            var invalidCount = await cleanupService.CleanupInvalidTokensAsync(cancellationToken);
            _logger.LogInformation("Proactive cleanup completed. Marked {Count} tokens as invalid", invalidCount);

            // Run stale token removal
            var staleCount = await cleanupService.RemoveStaleTokensAsync(cancellationToken);
            _logger.LogInformation("Stale token cleanup completed. Removed {Count} tokens", staleCount);

            _logger.LogInformation("Scheduled device token cleanup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled device token cleanup");
            // Don't throw - allow service to continue running
        }
    }
}

