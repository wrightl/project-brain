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
        // Seed roles
        if (!context.Roles.Any())
        {
            logger.LogInformation("Seeding roles...");

            var roles = new List<Role>
            {
                new()
                {
                    Name = "user",
                    Description = "Standard user with access to basic features",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    Name = "coach",
                    Description = "Coach user with access to coaching features and tools",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    Name = "admin",
                    Description = "Administrator with full system access and management capabilities",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await context.Roles.AddRangeAsync(roles, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Roles seeded successfully");
        }
        else
        {
            logger.LogInformation("Roles already exist, skipping seed");
        }

        // Seed subscription tiers
        if (!context.SubscriptionTiers.Any())
        {
            logger.LogInformation("Seeding subscription tiers...");

            // Use raw SQL to insert with explicit IDs (IDENTITY_INSERT)
            // Note: Escaping curly braces as {{}} because ExecuteSqlRawAsync treats the string as a format string
            var sql = @"
                SET IDENTITY_INSERT [SubscriptionTiers] ON;
                
                INSERT INTO [SubscriptionTiers] ([Id], [Name], [UserType], [Features])
                VALUES
                    (1, N'Free', N'user', N'{{}}'),
                    (2, N'Pro', N'user', N'{{}}'),
                    (3, N'Ultimate', N'user', N'{{}}'),
                    (4, N'Free', N'coach', N'{{}}'),
                    (5, N'Pro', N'coach', N'{{}}');
                
                SET IDENTITY_INSERT [SubscriptionTiers] OFF;
            ";

            await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);

            logger.LogInformation("Subscription tiers seeded successfully");
        }
        else
        {
            logger.LogInformation("Subscription tiers already exist, skipping seed");
        }

        // Seed subscription settings (singleton)
        if (!context.SubscriptionSettings.Any())
        {
            logger.LogInformation("Seeding subscription settings...");

            // Get first admin user or use a default system user ID
            var adminUser = await context.Users
                .Include(u => u.UserRoles)
                .Where(u => u.UserRoles.Any(ur => ur.RoleName == "admin"))
                .FirstOrDefaultAsync(cancellationToken);

            var updatedBy = adminUser?.Id ?? "system";
            var updatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            // Use raw SQL to insert with explicit ID (IDENTITY_INSERT)
            // Escape single quotes in the UpdatedBy value to prevent SQL injection
            var escapedUpdatedBy = updatedBy.Replace("'", "''");
            var sql = $@"
                SET IDENTITY_INSERT [SubscriptionSettings] ON;
                
                INSERT INTO [SubscriptionSettings] ([Id], [EnableUserSubscriptions], [EnableCoachSubscriptions], [UpdatedAt], [UpdatedBy])
                VALUES (1, 1, 1, N'{updatedAt}', N'{escapedUpdatedBy}');
                
                SET IDENTITY_INSERT [SubscriptionSettings] OFF;
            ";

            await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);

            logger.LogInformation("Subscription settings seeded successfully");
        }
        else
        {
            logger.LogInformation("Subscription settings already exist, skipping seed");
        }
    }
}