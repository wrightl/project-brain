using System.Diagnostics;
using System.Text.Json;
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

    private static Task EnsureDatabaseAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        // If you need to delete the database during development, uncomment this line
        // return context.Database.EnsureDeletedAsync(cancellationToken);
        return Task.CompletedTask;
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
        await SeedRolesAsync(context, cancellationToken);

        // Seed subscription tiers
        await SeedSubscriptionTiersAsync(context, cancellationToken);

        // Get first admin user
        User? adminUser = await SeedAdminUserAsync(context, identitySeedingService, configuration, cancellationToken);

        // Seed ApplicationSettings
        await SeedApplicationSettingsAsync(context, adminUser, cancellationToken);

        // Seed achievements (definitions/catalog)
        await SeedAchievementsAsync(context, cancellationToken);

        await SeedSystemTagsAsync(context, cancellationToken);
    }

    private async Task SeedAchievementsAsync(AppDbContext context, CancellationToken cancellationToken)
    {
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

    private async Task SeedApplicationSettingsAsync(AppDbContext context, User? adminUser, CancellationToken cancellationToken)
    {
        if (adminUser == null)
        {
            throw new InvalidOperationException("Admin user must exist before seeding ApplicationSettings.");
        }

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
            },
            new()
            {
                Key = "Referral:Enabled",
                Value = "true",
                Category = "Referral",
                Description = "Enable/disable the referral program"
            },
            new()
            {
                Key = "Referral:MaxRewardsPerInviter",
                Value = "12",
                Category = "Referral",
                Description = "Maximum number of referral rewards an inviter can receive"
            },
            new()
            {
                Key = "Referral:InviterFreeMonths",
                Value = "1",
                Category = "Referral",
                Description = "Number of free months awarded to the inviter per successful referral"
            },
            new()
            {
                Key = "Referral:InviteeFreeMonths",
                Value = "1",
                Category = "Referral",
                Description = "Number of free months awarded to the invitee when they become a paid subscriber"
            },
            new()
            {
                Key = "Referral:InviteTokenExpiryDays",
                Value = "30",
                Category = "Referral",
                Description = "Number of days before an invite link expires"
            },
            new()
            {
                Key = "Referral:MaxInvitesPerRequest",
                Value = "10",
                Category = "Referral",
                Description = "Maximum number of email addresses allowed per invite send"
            },
            new()
            {
                Key = "Referral:RequireInviterActiveSubscriberToEarn",
                Value = "false",
                Category = "Referral",
                Description = "Require inviter to be an active paid subscriber at the time rewards are granted"
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
                    UpdatedAt = DateTime.UtcNow,
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
    }

    private async Task<User?> SeedAdminUserAsync(AppDbContext context, IIdentitySeedingService identitySeedingService, IConfiguration configuration, CancellationToken cancellationToken)
    {
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
        else
        {
            logger.LogInformation("Admin user already exists in database with ID: {UserId}", adminUser.Id);
        }

        return adminUser;
    }

    private async Task SeedSubscriptionTiersAsync(AppDbContext context, CancellationToken cancellationToken)
    {
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
    }

    private async Task SeedRolesAsync(AppDbContext context, CancellationToken cancellationToken)
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
    }

    private async Task SeedSystemTagsAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        logger.LogInformation("Ensuring system tags exist...");

        var now = DateTime.UtcNow;

        var desired = new List<(string Key, string Name, string? Description, List<SystemTagFieldSeed> Fields)>
        {
            (
                "sleep",
                "Sleep",
                "Track sleep quality and how you felt on waking.",
                new List<SystemTagFieldSeed>
                {
                    new("sleepQualityRating", "How well did you sleep? (1–5)", "rating", isRequired: false, order: 1, min: 1, max: 5, step: 1),
                    new("hoursSlept", "How many hours did you sleep?", "number", isRequired: false, order: 2, min: 0, max: 24, step: 0.5m),
                    new("wakeFeeling", "How did you feel when you woke up?", "select", isRequired: false, order: 3, options: new[] { "Refreshed", "Okay", "Tired", "Exhausted" }),
                    new("sleepNotes", "Anything else about your sleep?", "textarea", isRequired: false, order: 4, placeholder: "Optional notes…"),
                }
            ),
            (
                "alcohol",
                "Alcohol",
                "Track alcohol use and how it affected you.",
                new List<SystemTagFieldSeed>
                {
                    new("units", "How many units/drinks?", "number", isRequired: false, order: 1, min: 0, max: 100, step: 0.5m),
                    new("type", "What did you drink?", "text", isRequired: false, order: 2, placeholder: "e.g. beer, wine…"),
                    new("timeLastDrink", "What time was your last drink?", "time", isRequired: false, order: 3),
                    new("nextDayEffect", "How did it affect you later/next day?", "select", isRequired: false, order: 4, options: new[] { "No noticeable effect", "Slightly worse", "Much worse", "Not sure" }),
                }
            ),
            (
                "anxiety",
                "Anxiety",
                "Track anxiety intensity and what might be driving it.",
                new List<SystemTagFieldSeed>
                {
                    new("intensity", "How intense was your anxiety? (1–10)", "number", isRequired: false, order: 1, min: 1, max: 10, step: 1),
                    new("primaryTrigger", "What do you think triggered it?", "text", isRequired: false, order: 2, placeholder: "Optional…"),
                    new("copingUsed", "What did you do to cope?", "textarea", isRequired: false, order: 3, placeholder: "Optional…"),
                }
            ),
            (
                "morningPages",
                "Morning Pages",
                "Track your daily free-writing practice.",
                new List<SystemTagFieldSeed>
                {
                    new("pages", "How many pages did you write?", "number", isRequired: false, order: 1, min: 0, max: 50, step: 0.5m),
                    new("durationMinutes", "How long did you write for? (minutes)", "number", isRequired: false, order: 2, min: 0, max: 600, step: 1),
                    new("feltAfter", "How did you feel afterwards?", "select", isRequired: false, order: 3, options: new[] { "Clearer", "Same", "More overwhelmed", "Not sure" }),
                    new("insight", "Any key insight?", "textarea", isRequired: false, order: 4, placeholder: "Optional…"),
                }
            ),
            (
                "gratitude",
                "Gratitude",
                "Capture things you feel grateful for today.",
                new List<SystemTagFieldSeed>
                {
                    new("item1", "Gratitude item 1", "text", isRequired: false, order: 1, placeholder: "…"),
                    new("item2", "Gratitude item 2", "text", isRequired: false, order: 2, placeholder: "…"),
                    new("item3", "Gratitude item 3", "text", isRequired: false, order: 3, placeholder: "…"),
                }
            ),
        };

        var existing = await context.SystemTags
            .Include(st => st.FieldDefinitions)
            .ToListAsync(cancellationToken);

        var existingByKey = existing.ToDictionary(st => st.Key, st => st, StringComparer.OrdinalIgnoreCase);

        var changedAny = false;

        foreach (var tag in desired)
        {
            if (!existingByKey.TryGetValue(tag.Key, out var existingTag))
            {
                existingTag = new SystemTag
                {
                    Id = Guid.NewGuid(),
                    Key = tag.Key,
                    Name = tag.Name,
                    Description = tag.Description,
                    CreatedAt = now,
                    UpdatedAt = now,
                    FieldDefinitions = new List<SystemTagFieldDefinition>()
                };
                context.SystemTags.Add(existingTag);
                existingByKey[tag.Key] = existingTag;
                changedAny = true;
            }
            else
            {
                // Keep seeded values up to date
                if (!string.Equals(existingTag.Name, tag.Name, StringComparison.Ordinal))
                {
                    existingTag.Name = tag.Name;
                    existingTag.UpdatedAt = now;
                    changedAny = true;
                }
                if (!string.Equals(existingTag.Description, tag.Description, StringComparison.Ordinal))
                {
                    existingTag.Description = tag.Description;
                    existingTag.UpdatedAt = now;
                    changedAny = true;
                }
            }

            var existingFieldsByKey = (existingTag.FieldDefinitions ?? new List<SystemTagFieldDefinition>())
                .ToDictionary(f => f.FieldKey, f => f, StringComparer.OrdinalIgnoreCase);

            foreach (var field in tag.Fields)
            {
                if (!existingFieldsByKey.TryGetValue(field.FieldKey, out var existingField))
                {
                    existingField = new SystemTagFieldDefinition
                    {
                        Id = Guid.NewGuid(),
                        SystemTagId = existingTag.Id,
                        FieldKey = field.FieldKey,
                        Label = field.Label,
                        InputType = field.InputType,
                        Required = field.Required,
                        FieldOrder = field.Order,
                        Placeholder = field.Placeholder,
                        Hint = field.Hint,
                        OptionsJson = field.Options != null ? JsonSerializer.Serialize(field.Options) : null,
                        MinValue = field.Min,
                        MaxValue = field.Max,
                        StepValue = field.Step,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    context.SystemTagFieldDefinitions.Add(existingField);
                    changedAny = true;
                }
                else
                {
                    // Update seeded definitions if changed
                    var optionsJson = field.Options != null ? JsonSerializer.Serialize(field.Options) : null;
                    var needsUpdate =
                        existingField.Label != field.Label ||
                        existingField.InputType != field.InputType ||
                        existingField.Required != field.Required ||
                        existingField.FieldOrder != field.Order ||
                        existingField.Placeholder != field.Placeholder ||
                        existingField.Hint != field.Hint ||
                        existingField.OptionsJson != optionsJson ||
                        existingField.MinValue != field.Min ||
                        existingField.MaxValue != field.Max ||
                        existingField.StepValue != field.Step;

                    if (needsUpdate)
                    {
                        existingField.Label = field.Label;
                        existingField.InputType = field.InputType;
                        existingField.Required = field.Required;
                        existingField.FieldOrder = field.Order;
                        existingField.Placeholder = field.Placeholder;
                        existingField.Hint = field.Hint;
                        existingField.OptionsJson = optionsJson;
                        existingField.MinValue = field.Min;
                        existingField.MaxValue = field.Max;
                        existingField.StepValue = field.Step;
                        existingField.UpdatedAt = now;
                        changedAny = true;
                    }
                }
            }
        }

        if (changedAny)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("System tags ensured successfully");
        }
        else
        {
            logger.LogInformation("System tags already up to date, skipping seed");
        }
    }
}

internal sealed class SystemTagFieldSeed
{
    public SystemTagFieldSeed(
        string fieldKey,
        string label,
        string inputType,
        bool isRequired,
        int order,
        string? placeholder = null,
        string? hint = null,
        IEnumerable<string>? options = null,
        decimal? min = null,
        decimal? max = null,
        decimal? step = null)
    {
        FieldKey = fieldKey;
        Label = label;
        InputType = inputType;
        Required = isRequired;
        Order = order;
        Placeholder = placeholder;
        Hint = hint;
        Options = options;
        Min = min;
        Max = max;
        Step = step;
    }

    public string FieldKey { get; }
    public string Label { get; }
    public string InputType { get; }
    public bool Required { get; }
    public int Order { get; }
    public string? Placeholder { get; }
    public string? Hint { get; }
    public IEnumerable<string>? Options { get; }
    public decimal? Min { get; }
    public decimal? Max { get; }
    public decimal? Step { get; }
}

public class SettingsKeysAndDefaultValues
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public required string Category { get; set; }
    public required string Description { get; set; }
}