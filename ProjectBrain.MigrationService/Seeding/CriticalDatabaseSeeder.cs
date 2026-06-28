using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectBrain.Shared.Constants;
using ProjectBrain.Database.Models;

namespace ProjectBrain.MigrationService.Seeding;

public static class CriticalDatabaseSeeder
{
    public static async Task SeedRolesAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!context.Roles.Any())
        {
            logger.LogInformation("Seeding roles...");

            var roles = new List<Role>
            {
                new()
                {
                    Name = AppRoles.User,
                    Description = "Standard user with access to basic features",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    Name = AppRoles.Coach,
                    Description = "Coach user with access to coaching features and tools",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    Name = AppRoles.Admin,
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
    }

    public static async Task SeedSubscriptionTiersAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!context.SubscriptionTiers.Any())
        {
            logger.LogInformation("Seeding subscription tiers...");

            if (context.Database.IsRelational())
            {
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
            }
            else
            {
                await context.SubscriptionTiers.AddRangeAsync(
                [
                    new SubscriptionTier { Id = 1, Name = "Free", UserType = "user", Features = "{}" },
                    new SubscriptionTier { Id = 2, Name = "Pro", UserType = "user", Features = "{}" },
                    new SubscriptionTier { Id = 3, Name = "Ultimate", UserType = "user", Features = "{}" },
                    new SubscriptionTier { Id = 4, Name = "Free", UserType = "coach", Features = "{}" },
                    new SubscriptionTier { Id = 5, Name = "Pro", UserType = "coach", Features = "{}" },
                ], cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }

            logger.LogInformation("Subscription tiers seeded successfully");
        }
        else
        {
            logger.LogInformation("Subscription tiers already exist, skipping seed");
        }
    }
}
