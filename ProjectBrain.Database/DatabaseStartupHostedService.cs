using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ProjectBrain.Database;

public class DatabaseStartupHostedService(
    IServiceProvider serviceProvider,
    IDatabaseStartupState startupState,
    ILogger<DatabaseStartupHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan MaxWarmupDuration = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(8);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (!await TryConnectWithRetriesAsync(context, stoppingToken))
            {
                logger.LogError(
                    "Database is not accessible during startup warmup after {ElapsedMilliseconds}ms",
                    sw.ElapsedMilliseconds);
                return;
            }

            startupState.MarkReady();
            logger.LogInformation("Database warmup completed after {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);

            var pending = await context.Database.GetPendingMigrationsAsync(stoppingToken);
            var pendingList = pending.ToList();
            if (pendingList.Count > 0)
            {
                logger.LogWarning(
                    "Database has {Count} pending migration(s): {Migrations}",
                    pendingList.Count,
                    string.Join(", ", pendingList));
                return;
            }

            startupState.MarkMigrationsApplied();
            logger.LogInformation("Database migrations verified after {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host is shutting down.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database startup warmup failed after {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
        }
    }

    private async Task<bool> TryConnectWithRetriesAsync(AppDbContext context, CancellationToken stoppingToken)
    {
        var deadline = DateTime.UtcNow + MaxWarmupDuration;
        var delay = InitialRetryDelay;
        var attempt = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            attempt++;
            try
            {
                if (await context.Database.CanConnectAsync(stoppingToken))
                {
                    var connection = context.Database.GetDbConnection();
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync(stoppingToken);
                    }

                    return true;
                }

                logger.LogWarning(
                    "Database CanConnect returned false on warmup attempt {Attempt}",
                    attempt);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Azure SQL serverless often returns transient errors (e.g. 40613) while resuming.
                logger.LogWarning(
                    ex,
                    "Database warmup attempt {Attempt} failed; will retry if within {MaxWarmupSeconds}s budget",
                    attempt,
                    MaxWarmupDuration.TotalSeconds);
            }

            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            var wait = delay < remaining ? delay : remaining;
            await Task.Delay(wait, stoppingToken);
            delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, MaxRetryDelay.TotalMilliseconds));
        }

        return false;
    }
}
