using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class ProjectBrainDbInitializer(IServiceProvider serviceProvider,
    ILogger<ProjectBrainDbInitializer> logger)
    : BackgroundService
{
    public const string ActivitySourceName = "Migrations";
    private readonly ActivitySource _activitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        using var activity = _activitySource.StartActivity("Initializing catalog database", ActivityKind.Client);
        await InitializeAsync(context, cancellationToken);
    }

    public async Task InitializeAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        await EnsureDatabaseAsync(context, cancellationToken);
        await RunMigrationAsync(context, cancellationToken);
        await SeedAsync(context, cancellationToken);

        logger.LogInformation("Database initialization completed after {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
    }

    private static async Task EnsureDatabaseAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        // If you need to delete the database during development, uncomment this line
        // await context.Database.EnsureDeletedAsync();
    }

    private static async Task RunMigrationAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await context.Database.MigrateAsync(cancellationToken);
        });
    }

    private async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        // No seeding required yet

        // // Check if data already exists
        // if (!context.Eggs.Any())
        // {
        //     // Add sample data
        //     var eggEntities = new List<Egg>
        //     {
        //         new() { Id = Guid.NewGuid(), Title = "Make Lee a cuppa", IsComplete= false },
        //     };

        //     await context.Eggs.AddRangeAsync(eggEntities);
        // }

        // // Save changes
        // await context.SaveChangesAsync();
    }
}