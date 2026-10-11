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

            await context.SubscriptionTiers.AddRangeAsync(
            [
                new SubscriptionTier { Id = 1, Name = "Free", UserType = "user", Features = "{}" },
                new SubscriptionTier { Id = 2, Name = "Pro", UserType = "user", Features = "{}" },
                new SubscriptionTier { Id = 3, Name = "Ultimate", UserType = "user", Features = "{}" },
                new SubscriptionTier { Id = 4, Name = "Free", UserType = "coach", Features = "{}" },
                new SubscriptionTier { Id = 5, Name = "Pro", UserType = "coach", Features = "{}" },
            ], cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            if (context.Database.IsRelational())
            {
                // Explicit ids do not advance the identity sequence; move it past them so the next insert does not collide.
                await context.Database.ExecuteSqlRawAsync(
                    """SELECT setval(pg_get_serial_sequence('"SubscriptionTiers"', 'Id'), (SELECT MAX("Id") FROM "SubscriptionTiers"))""",
                    cancellationToken);
            }

            logger.LogInformation("Subscription tiers seeded successfully");
        }
        else
        {
            logger.LogInformation("Subscription tiers already exist, skipping seed");
        }
    }

    public static async Task SeedCommunityChannelsAsync(
        AppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (await context.CommunityChannels.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Community channels already exist, skipping seed");
            return;
        }

        logger.LogInformation("Seeding community channels...");

        // Fixed ids so links to a channel are stable across environments
        await context.CommunityChannels.AddRangeAsync(
        [
            new CommunityChannel { Id = Guid.Parse("11111111-1111-1111-1111-111111111101"), Slug = "introductions", Name = "Introductions", Description = "Say hello and share a bit about yourself.", SortOrder = 1 },
            new CommunityChannel { Id = Guid.Parse("11111111-1111-1111-1111-111111111102"), Slug = "wins", Name = "Wins", Description = "Celebrate small and big wins.", SortOrder = 2 },
            new CommunityChannel { Id = Guid.Parse("11111111-1111-1111-1111-111111111103"), Slug = "struggles", Name = "Struggles", Description = "Share what's hard — you're not alone.", SortOrder = 3 },
            new CommunityChannel { Id = Guid.Parse("11111111-1111-1111-1111-111111111104"), Slug = "tips", Name = "Tips", Description = "Practical tips and strategies that help.", SortOrder = 4 },
        ], cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Community channels seeded successfully");
    }
}
