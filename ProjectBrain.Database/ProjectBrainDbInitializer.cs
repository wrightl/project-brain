using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectBrain.Database;
using ProjectBrain.Database.Constants;
using ProjectBrain.Database.Interfaces;
using ProjectBrain.Database.Models;

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
        var hostEnvironment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        using var activity = _activitySource.StartActivity("Initializing catalog database", ActivityKind.Client);
        await InitializeAsync(context, identitySeedingService, configuration, hostEnvironment, cancellationToken);
    }

    public async Task InitializeAsync(AppDbContext context, IIdentitySeedingService identitySeedingService, IConfiguration configuration, IHostEnvironment hostEnvironment, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        await EnsureDatabaseAsync(context, cancellationToken);
        await RunMigrationAsync(context, cancellationToken);
        await SeedAsync(context, identitySeedingService, configuration, hostEnvironment, cancellationToken);

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

    private async Task SeedAsync(AppDbContext context, IIdentitySeedingService identitySeedingService, IConfiguration configuration, IHostEnvironment hostEnvironment, CancellationToken cancellationToken)
    {
        // Seed roles
        await SeedRolesAsync(context, cancellationToken);

        // Seed subscription tiers
        await SeedSubscriptionTiersAsync(context, cancellationToken);

        // Get first admin user
        User? adminUser = await SeedAdminUserAsync(context, identitySeedingService, configuration, cancellationToken);

        try
        {
            await SeedTestUsersAsync(context, identitySeedingService, configuration, hostEnvironment, cancellationToken);
            await AcceptPendingFakeCoachConnectionsAsync(context, configuration, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed test users, skipping");
        }

        // Seed ApplicationSettings
        await SeedApplicationSettingsAsync(context, adminUser, cancellationToken);

        // Seed achievements (definitions/catalog)
        await SeedAchievementsAsync(context, cancellationToken);

        await SeedSystemTagsAsync(context, cancellationToken);

        await SeedCoachSpecialismOptionsAsync(context, cancellationToken);
        await SeedCountriesAsync(context, cancellationToken);
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
                Key = "AI:RecentMessageWindow",
                Value = "4",
                Category = "AI",
                Description = "Number of recent raw messages to include when a conversation summary is present"
            },
            new()
            {
                Key = "AI:ConversationSummaryInterval",
                Value = "6",
                Category = "AI",
                Description = "Regenerate conversation context summary every N persisted messages"
            },
            new()
            {
                Key = "AI:MaxConversationSummaryLength",
                Value = "1500",
                Category = "AI",
                Description = "Maximum stored conversation context summary length in characters"
            },
            new()
            {
                Key = "AI:EnableConversationSummary",
                Value = "true",
                Category = "AI",
                Description = "Enable rolling conversation context summaries in chat prompts"
            },
            new()
            {
                Key = "AI:Memory:EnableMemoryFormation",
                Value = "true",
                Category = "AI:Memory",
                Description = "Enable background extraction and storage of user facts and episodes from chat"
            },
            new()
            {
                Key = "AI:Memory:MinPromotionConfidence",
                Value = "0.75",
                Category = "AI:Memory",
                Description = "Minimum confidence to promote a memory candidate to active status"
            },
            new()
            {
                Key = "AI:Memory:ProvisionalConfidence",
                Value = "0.60",
                Category = "AI:Memory",
                Description = "Minimum confidence to store a memory candidate as provisional"
            },
            new()
            {
                Key = "AI:Memory:ActivationObservationCount",
                Value = "2",
                Category = "AI:Memory",
                Description = "Observations required to promote provisional memory to active"
            },
            new()
            {
                Key = "AI:Memory:MaxFactsPerTurn",
                Value = "3",
                Category = "AI:Memory",
                Description = "Maximum fact candidates extracted per chat turn"
            },
            new()
            {
                Key = "AI:Memory:MaxEpisodesPerTurn",
                Value = "2",
                Category = "AI:Memory",
                Description = "Maximum episode candidates extracted per chat turn"
            },
            new()
            {
                Key = "AI:Memory:MaxFactsRetrieved",
                Value = "5",
                Category = "AI:Memory",
                Description = "Maximum facts retrieved per chat turn for prompt injection"
            },
            new()
            {
                Key = "AI:Memory:MaxEpisodesRetrieved",
                Value = "3",
                Category = "AI:Memory",
                Description = "Maximum episodes retrieved per chat turn for prompt injection"
            },
            new()
            {
                Key = "AI:Memory:IndexProvisionalMemories",
                Value = "false",
                Category = "AI:Memory",
                Description = "Index provisional memories in search (default false; only active are retrieved)"
            },
            new()
            {
                Key = "AI:Policy:CrisisGuidance",
                Value = "Avoid medical or diagnostic claims. If the user describes immediate danger or crisis, encourage them to seek urgent professional help or contact local emergency services.",
                Category = "AI:Policy",
                Description = "Crisis and safety guardrails for chat responses"
            },
            new()
            {
                Key = "AI:Policy:CommunicationStyle",
                Value = "Be clear, concise, and break down complex information into manageable parts. Use a friendly, supportive, and respectful tone. If the query is unclear or ambiguous, politely ask for clarification. Use the user's name occasionally and naturally—not in every sentence, and never in a patronizing or condescending way.",
                Category = "AI:Policy",
                Description = "Communication style guardrails for chat responses"
            },
            new()
            {
                Key = "AI:Policy:CitationRules",
                Value = "Always cite sources using [number] format (e.g., [1], [2]) when referencing documents. Base responses on provided sources, the user's query, and conversation history. Ignore sources that are not relevant to the user's query or conversation history.",
                Category = "AI:Policy",
                Description = "Citation and source usage rules for chat responses"
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
                    .FirstOrDefaultAsync(u => u.UserRoles.Any(ur => ur.RoleName == AppRoles.Admin), cancellationToken);

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

            // // Get the admin role
            // var adminRole = await context.Roles
            //     .FirstOrDefaultAsync(r => r.Name == AppRoles.Admin, cancellationToken);

            // if (adminRole == null)
            // {
            //     logger.LogError("Admin role not found. Cannot create admin user.");
            //     throw new InvalidOperationException("Admin role must exist before creating admin user.");
            // }

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
                RoleName = AppRoles.Admin,
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

    public async Task SeedTestUsersFromEndpointAsync(
        IIdentitySeedingService identitySeedingService,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedTestUsersAsync(
            context,
            identitySeedingService,
            configuration,
            hostEnvironment,
            cancellationToken);
    }

    public async Task SeedTestUsersAsync(
        AppDbContext context,
        IIdentitySeedingService identitySeedingService,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        CancellationToken cancellationToken = default)
    {
        if (!ShouldSeedTestUsers(configuration, hostEnvironment))
        {
            logger.LogInformation("Test user seeding skipped (disabled or not configured for this environment).");
            return;
        }

        var section = configuration.GetSection("TestUsers");
        var password = section["Password"] ?? throw new InvalidOperationException("TestUsers:Password must be configured to seed test users. Set it via environment variable or user secrets.");
        var connection = section["Connection"] ?? DefaultTestUserConnection;

        logger.LogInformation("Seeding test users and coaches...");

        for (var i = 1; i <= 5; i++)
        {
            var fullName = $"TestUser{i}";
            var email = $"testuser{i}@{TestUsers.EmailDomain}";
            await EnsureTestUserSeededAsync(
                context,
                identitySeedingService,
                email,
                password,
                fullName,
                connection,
                AppRoles.User,
                isOnboarded: false,
                city: null,
                country: null,
                cancellationToken: cancellationToken);
        }

        foreach (var coach in TestCoachDefinitions)
        {
            var email = $"{SlugifyTestUserEmail(coach.FullName)}@{TestUsers.EmailDomain}";
            var auth0UserId = await EnsureTestUserSeededAsync(
                context,
                identitySeedingService,
                email,
                password,
                coach.FullName,
                connection,
                AppRoles.Coach,
                isOnboarded: true,
                city: coach.City,
                country: coach.Country,
                postalCode: coach.PostalCode,
                latitude: coach.Latitude,
                longitude: coach.Longitude,
                cancellationToken: cancellationToken);

            if (auth0UserId != null)
            {
                await EnsureCoachProfileSeededAsync(context, auth0UserId, cancellationToken);
            }
        }

        logger.LogInformation("Test user and coach seeding completed.");
    }

    private async Task AcceptPendingFakeCoachConnectionsAsync(
        AppDbContext context,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (!FakeCoachEnvironment.IsEnabled(configuration))
            return;

        var pendingConnections = await context.Connections
            .Include(c => c.Coach)
            .Where(c => c.Status == "pending")
            .ToListAsync(cancellationToken);

        var acceptedCount = 0;
        foreach (var connection in pendingConnections)
        {
            if (!TestUsers.IsTestCoachEmail(connection.Coach?.Email))
                continue;

            connection.Status = "accepted";
            connection.RespondedAt = DateTime.UtcNow;
            connection.UpdatedAt = DateTime.UtcNow;
            acceptedCount++;
        }

        if (acceptedCount > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Auto-accepted {Count} pending connection(s) to fake coaches.",
                acceptedCount);
        }
    }

    public static bool ShouldSeedTestUsers(IConfiguration configuration, IHostEnvironment? environment = null)
    {
        var section = configuration.GetSection("TestUsers");
        var password = section["Password"];
        if (string.IsNullOrWhiteSpace(password))
            return false;

        var deployEnv = configuration["deploy-env"] ?? configuration["DEPLOY_ENV"];
        if (string.Equals(deployEnv, "production", StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.Equals(deployEnv, "staging", StringComparison.OrdinalIgnoreCase))
            return true;

        if (environment?.IsDevelopment() == true)
            return section.GetValue<bool>("Enabled");

        if (environment?.IsProduction() == true && !section.GetValue<bool>("Enabled"))
            return false;

        return section.GetValue<bool>("Enabled");
    }

    private async Task<string?> EnsureTestUserSeededAsync(
        AppDbContext context,
        IIdentitySeedingService identitySeedingService,
        string email,
        string password,
        string fullName,
        string connection,
        string role,
        bool isOnboarded,
        string? city,
        string? country,
        string? postalCode = null,
        double? latitude = null,
        double? longitude = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existingDbUser = await context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (existingDbUser != null)
        {
            logger.LogInformation("Test account already exists in database: {Email}", email);
            return existingDbUser.Id;
        }

        var auth0UserId = await identitySeedingService.EnsureAuth0UserAsync(email, password, fullName, connection);
        var roleAssigned = await identitySeedingService.AssignAuth0RolesAsync(auth0UserId, [role]);
        if (!roleAssigned)
        {
            logger.LogWarning("Failed to assign {Role} role in Auth0 for {Email}", role, email);
        }

        var user = new User
        {
            Id = auth0UserId,
            Email = email,
            FullName = fullName,
            EmailVerified = true,
            IsOnboarded = isOnboarded,
            Connection = connection,
            City = city,
            Country = country,
            PostalCode = postalCode,
            Latitude = latitude,
            Longitude = longitude,
            LastActivityAt = string.Equals(role, AppRoles.Coach, StringComparison.OrdinalIgnoreCase)
                ? DateTime.UtcNow
                : null,
        };

        await context.Users.AddAsync(user, cancellationToken);
        await context.UserRoles.AddAsync(new UserRole
        {
            UserId = auth0UserId,
            RoleName = role,
            AssignedAt = DateTime.UtcNow,
        }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created test account {Email} with role {Role}, IsOnboarded={IsOnboarded}",
            email,
            role,
            isOnboarded);

        return auth0UserId;
    }

    private static async Task EnsureCoachProfileSeededAsync(
        AppDbContext context,
        string coachUserId,
        CancellationToken cancellationToken)
    {
        var profile = await context.CoachProfiles.FirstOrDefaultAsync(p => p.UserId == coachUserId, cancellationToken);
        if (profile == null)
        {
            profile = new CoachProfile
            {
                UserId = coachUserId,
                Bio = "Sample coach bio for development and testing.",
                AvailabilityStatus = AvailabilityStatus.Available,
            };
            await context.CoachProfiles.AddAsync(profile, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        if (!await context.CoachQualifications.AnyAsync(q => q.CoachProfileId == profile.Id, cancellationToken))
        {
            await context.CoachQualifications.AddRangeAsync(
                [
                    new CoachQualification { CoachProfileId = profile.Id, Qualification = "Sample qualification 1" },
                    new CoachQualification { CoachProfileId = profile.Id, Qualification = "Sample qualification 2" },
                ],
                cancellationToken);
        }

        if (!await context.CoachSpecialisms.AnyAsync(s => s.CoachProfileId == profile.Id, cancellationToken))
        {
            await context.CoachSpecialisms.AddRangeAsync(
                [
                    new CoachSpecialism { CoachProfileId = profile.Id, Specialism = "Anxiety" },
                    new CoachSpecialism { CoachProfileId = profile.Id, Specialism = "Stress" },
                ],
                cancellationToken);
        }

        if (!await context.CoachAgeGroups.AnyAsync(a => a.CoachProfileId == profile.Id, cancellationToken))
        {
            await context.CoachAgeGroups.AddRangeAsync(
                [
                    new CoachAgeGroup { CoachProfileId = profile.Id, AgeGroup = "Adults" },
                    new CoachAgeGroup { CoachProfileId = profile.Id, AgeGroup = "Young adults" },
                ],
                cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static string SlugifyTestUserEmail(string fullName)
    {
        var slug = fullName.ToLowerInvariant().Replace(' ', '.');
        foreach (var c in slug.Where(ch => !char.IsLetterOrDigit(ch) && ch != '.').ToList())
        {
            slug = slug.Replace(c.ToString(), string.Empty);
        }

        return slug.Trim('.');
    }

    private const string DefaultTestUserConnection = "Username-Password-Authentication";

    private static readonly TestCoachDefinition[] TestCoachDefinitions =
    [
        new("Sarah Mitchell", "London", "United Kingdom", "SW1A 1AA", 51.5014, -0.1419),
        new("James Okonkwo", "Manchester", "United Kingdom", "M1 1AD", 53.4808, -2.2426),
        new("Elena Vasquez", "Birmingham", "United Kingdom", "B2 4QA", 52.4796, -1.9027),
        new("Oliver Chen", "Leeds", "United Kingdom", "LS1 2TW", 53.7974, -1.5438),
        new("Amelia Brooks", "Bristol", "United Kingdom", "BS1 4ST", 51.4545, -2.5920),
        new("Noah Patel", "Edinburgh", "United Kingdom", "EH1 1BQ", 55.9533, -3.1883),
        new("Isla Fraser", "Glasgow", "United Kingdom", "G2 3BZ", 55.8611, -4.2500),
        new("Lucas Bergström", "Cardiff", "United Kingdom", "CF10 3AT", 51.4816, -3.1791),
        new("Maya Singh", "Belfast", "United Kingdom", "BT1 5GS", 54.5969, -5.9263),
        new("Ethan O'Connor", "Cambridge", "United Kingdom", "CB2 1RP", 52.2044, 0.1149),
    ];

    private sealed record TestCoachDefinition(
        string FullName,
        string City,
        string Country,
        string PostalCode,
        double Latitude,
        double Longitude);

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

    private async Task SeedCoachSpecialismOptionsAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        logger.LogInformation("Ensuring coach specialism options exist...");

        var existingNames = await context.CoachSpecialismOptions
            .Select(o => o.Name)
            .ToListAsync(cancellationToken);
        var existingSet = existingNames.ToHashSet(StringComparer.Ordinal);

        var toAdd = new List<CoachSpecialismOption>();
        for (var i = 0; i < CoachSpecialismCatalog.DefaultOptions.Count; i++)
        {
            var name = CoachSpecialismCatalog.DefaultOptions[i];
            if (!existingSet.Contains(name))
            {
                toAdd.Add(new CoachSpecialismOption
                {
                    Name = name,
                    SortOrder = i + 1,
                    IsActive = true,
                });
            }
        }

        if (toAdd.Count > 0)
        {
            await context.CoachSpecialismOptions.AddRangeAsync(toAdd, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} coach specialism options", toAdd.Count);
        }
        else
        {
            logger.LogInformation("Coach specialism options already up to date");
        }
    }

    private async Task SeedCountriesAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        logger.LogInformation("Ensuring countries exist...");

        var existingCodes = await context.Countries
            .Select(c => c.Code)
            .ToListAsync(cancellationToken);
        var existingSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = new List<Country>();
        foreach (var (name, code) in CountryCatalog.DefaultCountries)
        {
            if (!existingSet.Contains(code))
            {
                toAdd.Add(new Country
                {
                    Name = name,
                    Code = code,
                    IsActive = true,
                });
            }
        }

        if (toAdd.Count > 0)
        {
            await context.Countries.AddRangeAsync(toAdd, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} countries", toAdd.Count);
        }
        else
        {
            logger.LogInformation("Countries already up to date");
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