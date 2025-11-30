using Microsoft.AspNetCore.Mvc;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;

public class SubscriptionServices(
    ILogger<SubscriptionServices> logger,
    IIdentityService identityService,
    ISubscriptionService subscriptionService,
    IUsageTrackingService usageTrackingService,
    IFeatureGateService featureGateService)
{
    public ILogger<SubscriptionServices> Logger { get; } = logger;
    public IIdentityService IdentityService { get; } = identityService;
    public ISubscriptionService SubscriptionService { get; } = subscriptionService;
    public IUsageTrackingService UsageTrackingService { get; } = usageTrackingService;
    public IFeatureGateService FeatureGateService { get; } = featureGateService;
}

public static class SubscriptionEndpoints
{
    public static void MapSubscriptionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("subscriptions").RequireAuthorization();

        // Get current subscription
        group.MapGet("/me", GetMySubscription).WithName("GetMySubscription");

        // Create checkout session
        group.MapPost("/checkout", CreateCheckout).WithName("CreateCheckout");

        // Cancel subscription
        group.MapPost("/cancel", CancelSubscription).WithName("CancelSubscription");

        // Start trial
        group.MapPost("/trial", StartTrial).WithName("StartTrial");

        // Get usage
        group.MapGet("/usage", GetUsage).WithName("GetUsage");

        // Get tier
        group.MapGet("/tier", GetTier).WithName("GetTier");
    }

    private static async Task<IResult> GetMySubscription([AsParameters] SubscriptionServices services)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        // Determine user type from roles
        var isCoach = services.IdentityService.IsCoach;
        var userType = isCoach ? "coach" : "user";

        try
        {
            var subscription = await services.SubscriptionService.GetUserSubscriptionAsync(userId, userType);

            if (subscription == null)
            {
                return Results.Ok(new
                {
                    tier = "Free",
                    status = "active",
                    userType = userType
                });
            }

            return Results.Ok(new
            {
                id = subscription.Id,
                tier = subscription.Tier?.Name ?? "Free",
                status = subscription.Status,
                trialEndsAt = subscription.TrialEndsAt,
                currentPeriodStart = subscription.CurrentPeriodStart,
                currentPeriodEnd = subscription.CurrentPeriodEnd,
                canceledAt = subscription.CanceledAt,
                userType = subscription.UserType
            });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving subscription for user {UserId}", userId);
            return Results.Problem("An error occurred while retrieving subscription");
        }
    }

    private static async Task<IResult> CreateCheckout(
        [AsParameters] SubscriptionServices services,
        CreateCheckoutRequest request)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var isCoach = services.IdentityService.IsCoach;
        var userType = isCoach ? "coach" : "user";

        try
        {
            var checkoutUrl = await services.SubscriptionService.CreateCheckoutSessionAsync(
                userId, userType, request.Tier, request.IsAnnual);

            return Results.Ok(new { url = checkoutUrl });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error creating checkout session for user {UserId}", userId);
            return Results.Problem("An error occurred while creating checkout session");
        }
    }

    private static async Task<IResult> CancelSubscription([AsParameters] SubscriptionServices services)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var isCoach = services.IdentityService.IsCoach;
        var userType = isCoach ? "coach" : "user";

        try
        {
            await services.SubscriptionService.CancelSubscriptionAsync(userId, userType);
            return Results.Ok(new { message = "Subscription canceled successfully" });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error canceling subscription for user {UserId}", userId);
            return Results.Problem("An error occurred while canceling subscription");
        }
    }

    private static async Task<IResult> StartTrial(
        [AsParameters] SubscriptionServices services,
        StartTrialRequest request)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var isCoach = services.IdentityService.IsCoach;
        var userType = isCoach ? "coach" : "user";

        try
        {
            await services.SubscriptionService.StartTrialAsync(userId, userType, request.Tier);
            return Results.Ok(new { message = "Trial started successfully" });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error starting trial for user {UserId}", userId);
            return Results.Problem(ex.Message);
        }
    }

    private static async Task<IResult> GetUsage([AsParameters] SubscriptionServices services)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var isCoach = services.IdentityService.IsCoach;
        var userType = isCoach ? "coach" : "user";

        try
        {
            var dailyAIQueries = await services.UsageTrackingService.GetUsageCountAsync(userId, "ai_query", "daily");
            var monthlyAIQueries = await services.UsageTrackingService.GetUsageCountAsync(userId, "ai_query", "monthly");
            var monthlyCoachMessages = await services.UsageTrackingService.GetUsageCountAsync(userId, "coach_message", "monthly");
            var monthlyClientMessages = await services.UsageTrackingService.GetUsageCountAsync(userId, "client_message", "monthly");
            var fileStorage = await services.UsageTrackingService.GetFileStorageUsageAsync(userId);
            var monthlyResearchReports = await services.UsageTrackingService.GetUsageCountAsync(userId, "research_report", "monthly");

            return Results.Ok(new
            {
                aiQueries = new
                {
                    daily = dailyAIQueries,
                    monthly = monthlyAIQueries
                },
                coachMessages = new
                {
                    monthly = monthlyCoachMessages
                },
                clientMessages = new
                {
                    monthly = monthlyClientMessages
                },
                fileStorage = new
                {
                    bytes = fileStorage,
                    megabytes = fileStorage / (1024.0 * 1024.0)
                },
                researchReports = new
                {
                    monthly = monthlyResearchReports
                }
            });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving usage for user {UserId}", userId);
            return Results.Problem("An error occurred while retrieving usage");
        }
    }

    private static async Task<IResult> GetTier([AsParameters] SubscriptionServices services)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var isCoach = services.IdentityService.IsCoach;
        var userType = isCoach ? "coach" : "user";

        try
        {
            var tier = await services.SubscriptionService.GetUserTierAsync(userId, userType);
            return Results.Ok(new { tier, userType });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving tier for user {UserId}", userId);
            return Results.Problem("An error occurred while retrieving tier");
        }
    }
}

public class CreateCheckoutRequest
{
    public required string Tier { get; init; }
    public bool IsAnnual { get; init; }
}

public class StartTrialRequest
{
    public required string Tier { get; init; }
}

