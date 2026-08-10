using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using ProjectBrain.Database;

public class DatabaseMigrationsHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDatabaseStartupState _startupState;
    private readonly ILogger<DatabaseMigrationsHealthCheck> _logger;

    public DatabaseMigrationsHealthCheck(
        IServiceProvider serviceProvider,
        IDatabaseStartupState startupState,
        ILogger<DatabaseMigrationsHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _startupState = startupState;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_startupState.IsWarmedUp)
            {
                return HealthCheckResult.Unhealthy("Database warmup not complete");
            }

            if (_startupState.AreMigrationsApplied)
            {
                // Status-only after warmup: avoid CanConnectAsync so ops hits do not
                // keep serverless SQL awake.
                return HealthCheckResult.Healthy("All migrations have been applied");
            }

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var pendingMigrationsList = (await dbContext.Database
                .GetPendingMigrationsAsync(cancellationToken)).ToList();

            if (pendingMigrationsList.Count > 0)
            {
                _logger.LogWarning(
                    "Database has {Count} pending migration(s): {Migrations}",
                    pendingMigrationsList.Count,
                    string.Join(", ", pendingMigrationsList));

                return HealthCheckResult.Unhealthy(
                    $"Database has {pendingMigrationsList.Count} pending migration(s): {string.Join(", ", pendingMigrationsList)}");
            }

            _startupState.MarkMigrationsApplied();
            return HealthCheckResult.Healthy("All migrations have been applied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking database migrations health");
            return HealthCheckResult.Unhealthy("Error checking database migrations", ex);
        }
    }
}
