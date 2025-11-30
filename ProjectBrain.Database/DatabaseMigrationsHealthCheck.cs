using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

public class DatabaseMigrationsHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMigrationsHealthCheck> _logger;

    public DatabaseMigrationsHealthCheck(
        IServiceProvider serviceProvider,
        ILogger<DatabaseMigrationsHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Check if database can be accessed
            if (!await dbContext.Database.CanConnectAsync(cancellationToken))
            {
                return HealthCheckResult.Unhealthy("Database is not accessible");
            }

            // Check if there are any pending migrations
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            var pendingMigrationsList = pendingMigrations.ToList();

            if (pendingMigrationsList.Count > 0)
            {
                _logger.LogWarning(
                    "Database has {Count} pending migration(s): {Migrations}",
                    pendingMigrationsList.Count,
                    string.Join(", ", pendingMigrationsList));

                return HealthCheckResult.Unhealthy(
                    $"Database has {pendingMigrationsList.Count} pending migration(s): {string.Join(", ", pendingMigrationsList)}");
            }

            return HealthCheckResult.Healthy("All migrations have been applied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking database migrations health");
            return HealthCheckResult.Unhealthy("Error checking database migrations", ex);
        }
    }
}

