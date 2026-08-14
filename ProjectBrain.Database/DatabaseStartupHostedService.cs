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
    internal TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromSeconds(1);
    internal TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(8);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var delay = InitialRetryDelay;
        var attempt = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            attempt++;
            try
            {
                // New scope per attempt: a failed CanConnect can leave the same
                // DbContext/connection unusable even after SQL has resumed.
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (!await TryOpenConnectionAsync(context, stoppingToken))
                {
                    logger.LogWarning(
                        "Database CanConnect returned false on warmup attempt {Attempt}",
                        attempt);
                }
                else
                {
                    startupState.MarkReady();
                    logger.LogInformation(
                        "Database warmup completed after {ElapsedMilliseconds}ms on attempt {Attempt}",
                        sw.ElapsedMilliseconds,
                        attempt);

                    await VerifyMigrationsAsync(context, stoppingToken);
                    return;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Azure SQL serverless often returns transient errors (e.g. 40613) while resuming.
                // Keep retrying for the life of the process: giving up leaves the startup gate
                // returning 503 forever while /health still succeeds, so ACA never recycles the replica.
                logger.LogWarning(
                    ex,
                    "Database warmup attempt {Attempt} failed after {ElapsedMilliseconds}ms; will keep retrying",
                    attempt,
                    sw.ElapsedMilliseconds);
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, MaxRetryDelay.TotalMilliseconds));
        }
    }

    private static Task<bool> TryOpenConnectionAsync(AppDbContext context, CancellationToken stoppingToken)
        => context.Database.CanConnectAsync(stoppingToken);

    private async Task VerifyMigrationsAsync(AppDbContext context, CancellationToken stoppingToken)
    {
        try
        {
            var pendingList = (await context.Database.GetPendingMigrationsAsync(stoppingToken)).ToList();
            if (pendingList.Count > 0)
            {
                logger.LogWarning(
                    "Database has {Count} pending migration(s): {Migrations}",
                    pendingList.Count,
                    string.Join(", ", pendingList));
                return;
            }

            startupState.MarkMigrationsApplied();
            logger.LogInformation("Database migrations verified");
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Database migrations verification failed after warmup");
        }
    }
}
