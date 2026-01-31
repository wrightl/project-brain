using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectBrain.Database.Interfaces;

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
        var identitySeedingService = scope.ServiceProvider.GetRequiredService<IIdentitySeedingService>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        using var activity = _activitySource.StartActivity("Initializing catalog database", ActivityKind.Client);
        await InitializeAsync(context, identitySeedingService, configuration, cancellationToken);
    }

    public async Task InitializeAsync(AppDbContext context, IIdentitySeedingService identitySeedingService, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        await EnsureDatabaseAsync(context, cancellationToken);
        await RunMigrationAsync(context, cancellationToken);
        await SeedAsync(context, identitySeedingService, configuration, cancellationToken);

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

    private async Task SeedAsync(AppDbContext context, IIdentitySeedingService identitySeedingService, IConfiguration configuration, CancellationToken cancellationToken)
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

        // Get first admin user
        var adminUser = await context.Users
            .FirstOrDefaultAsync(u => u.UserRoles.Any(ur => ur.RoleName == "admin"), cancellationToken);

        // If no admin user exists, create a system user
        if (adminUser == null)
        {
            logger.LogInformation("No admin user found. Creating admin user...");

            // Read AdminUser configuration
            var adminConfig = configuration.GetSection("AdminUser");
            var adminEmail = adminConfig["Email"] ?? "system@projectbrain.internal";
            var adminPassword = adminConfig["Password"] ?? throw new InvalidOperationException("AdminUser:Password must be configured to create admin user. Set it via environment variable or user secrets.");
            var adminFullName = adminConfig["FullName"] ?? "System Administrator";
            var adminConnection = adminConfig["Connection"] ?? "Username-Password-Authentication";

            string auth0UserId = await identitySeedingService.EnsureAdminUserSeededAsync(adminEmail, adminPassword, adminFullName, adminConnection);

            // Get the admin role
            var adminRole = await context.Roles
                .FirstOrDefaultAsync(r => r.Name == "admin", cancellationToken);

            if (adminRole == null)
            {
                logger.LogError("Admin role not found. Cannot create admin user.");
                throw new InvalidOperationException("Admin role must exist before creating admin user.");
            }

            // Create admin user in database
            var systemUser = new User
            {
                Id = auth0UserId,
                Email = adminEmail,
                FullName = adminFullName,
                EmailVerified = true,
                IsOnboarded = true,
                Connection = adminConnection
            };

            await context.Users.AddAsync(systemUser, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Admin user created in database with ID: {UserId}", auth0UserId);

            // Assign admin role to system user in database
            var systemUserRole = new UserRole
            {
                UserId = auth0UserId,
                RoleName = "admin",
                AssignedAt = DateTime.UtcNow
            };

            await context.UserRoles.AddAsync(systemUserRole, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Admin role assigned to user in database.");
            adminUser = systemUser;
        }

        // // Seed application settings (AI settings)
        // if (!context.ApplicationSettings.Any())
        // {
        //     logger.LogInformation("Seeding application settings...");

        //     var updatedBy = adminUser.Id;
        //     var updatedAt = DateTime.UtcNow;

        //     // Seed AI settings with default values from appsettings.json
        //     var aiSettings = new List<ApplicationSetting>
        //     {
        //         new()
        //         {
        //             Key = "AI:MaxSearchResults",
        //             Value = "5",
        //             Category = "AI",
        //             Description = "Maximum number of search results to return",
        //             UpdatedAt = updatedAt,
        //             UpdatedBy = updatedBy
        //         },
        //         new()
        //         {
        //             Key = "AI:MaxContentLengthPerSource",
        //             Value = "800",
        //             Category = "AI",
        //             Description = "Maximum content length per source in characters",
        //             UpdatedAt = updatedAt,
        //             UpdatedBy = updatedBy
        //         },
        //         new()
        //         {
        //             Key = "AI:MaxHistoryMessages",
        //             Value = "10",
        //             Category = "AI",
        //             Description = "Maximum number of history messages to include",
        //             UpdatedAt = updatedAt,
        //             UpdatedBy = updatedBy
        //         },
        //         new()
        //         {
        //             Key = "AI:MaxTotalTokens",
        //             Value = "7000",
        //             Category = "AI",
        //             Description = "Maximum total tokens allowed",
        //             UpdatedAt = updatedAt,
        //             UpdatedBy = updatedBy
        //         }
        //     };

        //     await context.ApplicationSettings.AddRangeAsync(aiSettings, cancellationToken);
        //     await context.SaveChangesAsync(cancellationToken);

        //     logger.LogInformation("Application settings seeded successfully");
        // }
        // else
        // {
        //     logger.LogInformation("Application settings already exist, skipping seed");
        // }

        // Seed subscription settings into ApplicationSettings (always ensure keys exist)

        var settingsKeysAndDefaultValues = new List<SettingsKeysAndDefaultValues>
        {
            new()
            {
                Key = "Subscription:EnableUserSubscriptions",
                Value = "true",
                Category = "Subscription",
                Description = "Enable/disable subscriptions for regular users"
            },
            new()
            {
                Key = "Subscription:EnableCoachSubscriptions",
                Value = "true",
                Category = "Subscription",
                Description = "Enable/disable subscriptions for coaches"
            },
            new()
            {
                Key = "AI:MaxSearchResults",
                Value = "5",
                Category = "AI",
                Description = "Maximum number of search results to return"
            },
            new()
            {
                Key = "AI:MaxContentLengthPerSource",
                Value = "800",
                Category = "AI",
                Description = "Maximum content length per source in characters"
            },
            new()
            {
                Key = "AI:MaxHistoryMessages",
                Value = "10",
                Category = "AI",
                Description = "Maximum number of history messages to include"
            },
            new()
            {
                Key = "AI:MaxTotalTokens",
                Value = "7000",
                Category = "AI",
                Description = "Maximum total tokens allowed"
            }
        };

        var existingKeys = await context.ApplicationSettings
            .Select(s => s.Key)
            .ToListAsync(cancellationToken);

        var addedSettingsAny = false;

        foreach (var setting in settingsKeysAndDefaultValues)
        {
            if (!existingKeys.Contains(setting.Key))
            {
                context.ApplicationSettings.Add(new ApplicationSetting
                {
                    Key = setting.Key,
                    Value = setting.Value,
                    Category = setting.Category,
                    Description = setting.Description,
                    UpdatedAt = DateTime.Now,
                    UpdatedBy = adminUser.Id
                });
                addedSettingsAny = true;
            }
        }

        if (addedSettingsAny)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Subscription settings ensured successfully");
        }
        else
        {
            logger.LogInformation("Subscription settings already exist, skipping seed");
        }

        // Seed achievements (definitions/catalog)
        if (!context.Achievements.Any())
        {
            logger.LogInformation("Seeding achievements...");

            var achievements = new List<Achievement>
            {
                new()
                {
                    Key = "streak_1",
                    Title = "First streak",
                    Description = "Complete all your goals for a day.",
                    IconKey = "trophy",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    Key = "streak_3",
                    Title = "3-day streak",
                    Description = "Complete all your goals for 3 days in a row.",
                    IconKey = "trophy",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    Key = "streak_7",
                    Title = "7-day streak",
                    Description = "Complete all your goals for 7 days in a row.",
                    IconKey = "trophy",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    Key = "first_coach_connection",
                    Title = "Connected",
                    Description = "Connect with your first coach.",
                    IconKey = "trophy",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
            };

            await context.Achievements.AddRangeAsync(achievements, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Achievements seeded successfully");
        }
        else
        {
            logger.LogInformation("Achievements already exist, skipping seed");
        }
    }
}

public class SettingsKeysAndDefaultValues
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public required string Category { get; set; }
    public required string Description { get; set; }
}