using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;

public class SubscriptionManagementServices(
    ILogger<SubscriptionManagementServices> logger,
    IIdentityService identityService,
    ISubscriptionService subscriptionService)
{
    public ILogger<SubscriptionManagementServices> Logger { get; } = logger;
    public IIdentityService IdentityService { get; } = identityService;
    public ISubscriptionService SubscriptionService { get; } = subscriptionService;
}

public static class SubscriptionManagementEndpoints
{
    public static void MapSubscriptionManagementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("admin/subscriptions").RequireAuthorization("AdminOnly");

        // Subscription settings
        group.MapGet("/settings", GetSubscriptionSettings).WithName("GetSubscriptionSettings");
        group.MapPut("/settings", UpdateSubscriptionSettings).WithName("UpdateSubscriptionSettings");

        // User exclusions
        group.MapGet("/exclusions", GetExclusions).WithName("GetExclusions");
        group.MapPost("/exclusions", AddExclusion).WithName("AddExclusion");
        group.MapDelete("/exclusions/{userId}", RemoveExclusion).WithName("RemoveExclusion");

        // Get all subscriptions
        group.MapGet("/all", GetAllSubscriptions).WithName("GetAllSubscriptions");

        // Get user subscription (admin only)
        group.MapGet("/user/{userId}", GetUserSubscription).WithName("GetUserSubscription");
    }

    private static async Task<IResult> GetSubscriptionSettings([AsParameters] SubscriptionManagementServices services)
    {
        try
        {
            var settings = await services.SubscriptionService.GetSubscriptionSettingsAsync();
            return Results.Ok(new
            {
                enableUserSubscriptions = settings.EnableUserSubscriptions,
                enableCoachSubscriptions = settings.EnableCoachSubscriptions,
                updatedAt = settings.UpdatedAt,
                updatedBy = settings.UpdatedBy
            });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving subscription settings");
            return Results.Problem("An error occurred while retrieving subscription settings");
        }
    }

    private static async Task<IResult> UpdateSubscriptionSettings(
        [AsParameters] SubscriptionManagementServices services,
        UpdateSubscriptionSettingsRequest request)
    {
        var adminId = services.IdentityService.UserId!;
        try
        {
            await services.SubscriptionService.UpdateSubscriptionSettingsAsync(
                request.EnableUserSubscriptions,
                request.EnableCoachSubscriptions,
                adminId);

            return Results.Ok(new { message = "Settings updated successfully" });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error updating subscription settings");
            return Results.Problem("An error occurred while updating subscription settings");
        }
    }

    private static async Task<IResult> GetExclusions([AsParameters] SubscriptionManagementServices services)
    {
        // This would need a service method to get all exclusions
        // For now, return empty list
        return Results.Ok(new List<object>());
    }

    private static async Task<IResult> AddExclusion(
        [AsParameters] SubscriptionManagementServices services,
        AddExclusionRequest request)
    {
        var adminId = services.IdentityService.UserId ?? throw new Exception("Admin ID is required");

        try
        {
            await services.SubscriptionService.ExcludeUserFromSubscriptionAsync(
                request.UserId,
                request.UserType,
                adminId,
                request.Notes);

            return Results.Ok(new { message = "User excluded successfully" });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error adding exclusion");
            return Results.Problem("An error occurred while adding exclusion");
        }
    }

    private static async Task<IResult> RemoveExclusion(
        [AsParameters] SubscriptionManagementServices services,
        string userId)
    {
        try
        {
            // Need to determine user type - for now, try both
            await services.SubscriptionService.RemoveExclusionAsync(userId, "user");
            await services.SubscriptionService.RemoveExclusionAsync(userId, "coach");

            return Results.Ok(new { message = "Exclusion removed successfully" });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error removing exclusion");
            return Results.Problem("An error occurred while removing exclusion");
        }
    }

    private static async Task<IResult> GetAllSubscriptions([AsParameters] SubscriptionManagementServices services)
    {
        // This would need a service method to get all subscriptions
        // For now, return empty list
        return Results.Ok(new List<object>());
    }

    private static async Task<IResult> GetUserSubscription(
        [AsParameters] SubscriptionManagementServices services,
        string userId)
    {
        try
        {
            // Try both user types
            var userSubscription = await services.SubscriptionService.GetUserSubscriptionAsync(userId, "user");
            if (userSubscription == null)
            {
                userSubscription = await services.SubscriptionService.GetUserSubscriptionAsync(userId, "coach");
            }

            // Check if user is excluded
            var isExcludedUser = await services.SubscriptionService.IsUserExcludedAsync(userId, "user");
            var isExcludedCoach = await services.SubscriptionService.IsUserExcludedAsync(userId, "coach");
            var isExcluded = isExcludedUser || isExcludedCoach;

            if (userSubscription == null && !isExcluded)
            {
                return Results.Ok(new
                {
                    tier = "Free",
                    status = "active",
                    userType = "user",
                    isExcluded = false
                });
            }

            if (isExcluded)
            {
                return Results.Ok(new
                {
                    tier = "Free",
                    status = "active",
                    userType = userSubscription?.UserType ?? "user",
                    isExcluded = true
                });
            }

            return Results.Ok(new
            {
                id = userSubscription.Id,
                tier = userSubscription.Tier?.Name ?? "Free",
                status = userSubscription.Status,
                trialEndsAt = userSubscription.TrialEndsAt,
                currentPeriodStart = userSubscription.CurrentPeriodStart,
                currentPeriodEnd = userSubscription.CurrentPeriodEnd,
                canceledAt = userSubscription.CanceledAt,
                userType = userSubscription.UserType,
                isExcluded = false
            });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving subscription for user {UserId}", userId);
            return Results.Problem("An error occurred while retrieving subscription");
        }
    }
}

public class UpdateSubscriptionSettingsRequest
{
    public bool EnableUserSubscriptions { get; init; }
    public bool EnableCoachSubscriptions { get; init; }
}

public class AddExclusionRequest
{
    public required string UserId { get; init; } = string.Empty;
    public required string UserType { get; init; } = string.Empty;
    public string? Notes { get; init; }
}

