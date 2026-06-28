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
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (!await context.Database.CanConnectAsync(stoppingToken))
            {
                logger.LogError("Database is not accessible during startup warmup");
                return;
            }

            var connection = context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(stoppingToken);
            }

            startupState.MarkReady();
            logger.LogInformation("Database warmup completed after {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database startup warmup failed after {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
        }
    }
}
