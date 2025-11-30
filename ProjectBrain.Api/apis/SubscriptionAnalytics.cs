using Microsoft.AspNetCore.Mvc;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;

public class SubscriptionAnalyticsServices(
    ILogger<SubscriptionAnalyticsServices> logger,
    IIdentityService identityService,
    ISubscriptionAnalyticsService analyticsService)
{
    public ILogger<SubscriptionAnalyticsServices> Logger { get; } = logger;
    public IIdentityService IdentityService { get; } = identityService;
    public ISubscriptionAnalyticsService AnalyticsService { get; } = analyticsService;
}

public static class SubscriptionAnalyticsEndpoints
{
    public static void MapSubscriptionAnalyticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("admin/subscriptions/analytics").RequireAuthorization("AdminOnly");

        group.MapGet("/paid-subscribers", GetPaidSubscribers).WithName("GetPaidSubscribers");
        group.MapGet("/cancelled", GetCancelledSubscriptions).WithName("GetCancelledSubscriptions");
        group.MapGet("/expired", GetExpiredSubscriptions).WithName("GetExpiredSubscriptions");
        group.MapGet("/revenue", GetRevenue).WithName("GetRevenue");
        group.MapGet("/revenue/history", GetRevenueHistory).WithName("GetRevenueHistory");
        group.MapGet("/revenue/predicted", GetPredictedRevenue).WithName("GetPredictedRevenue");
        group.MapGet("/by-tier", GetSubscriptionsByTier).WithName("GetSubscriptionsByTier");
    }

    private static async Task<IResult> GetPaidSubscribers(
        [AsParameters] SubscriptionAnalyticsServices services,
        [FromQuery] string? userType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var results = new Dictionary<string, int>();
            
            if (string.IsNullOrEmpty(userType) || userType == "user")
            {
                results["user"] = await services.AnalyticsService.GetPaidSubscribersCountAsync("user", startDate, endDate);
            }
            
            if (string.IsNullOrEmpty(userType) || userType == "coach")
            {
                results["coach"] = await services.AnalyticsService.GetPaidSubscribersCountAsync("coach", startDate, endDate);
            }

            return Results.Ok(results);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving paid subscribers count");
            return Results.Problem("An error occurred while retrieving paid subscribers count");
        }
    }

    private static async Task<IResult> GetCancelledSubscriptions(
        [AsParameters] SubscriptionAnalyticsServices services,
        [FromQuery] string? userType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var results = new Dictionary<string, int>();
            
            if (string.IsNullOrEmpty(userType) || userType == "user")
            {
                results["user"] = await services.AnalyticsService.GetCancelledSubscriptionsCountAsync("user", startDate, endDate);
            }
            
            if (string.IsNullOrEmpty(userType) || userType == "coach")
            {
                results["coach"] = await services.AnalyticsService.GetCancelledSubscriptionsCountAsync("coach", startDate, endDate);
            }

            return Results.Ok(results);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving cancelled subscriptions count");
            return Results.Problem("An error occurred while retrieving cancelled subscriptions count");
        }
    }

    private static async Task<IResult> GetExpiredSubscriptions(
        [AsParameters] SubscriptionAnalyticsServices services,
        [FromQuery] string? userType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var results = new Dictionary<string, int>();
            
            if (string.IsNullOrEmpty(userType) || userType == "user")
            {
                results["user"] = await services.AnalyticsService.GetExpiredSubscriptionsCountAsync("user", startDate, endDate);
            }
            
            if (string.IsNullOrEmpty(userType) || userType == "coach")
            {
                results["coach"] = await services.AnalyticsService.GetExpiredSubscriptionsCountAsync("coach", startDate, endDate);
            }

            return Results.Ok(results);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving expired subscriptions count");
            return Results.Problem("An error occurred while retrieving expired subscriptions count");
        }
    }

    private static async Task<IResult> GetRevenue(
        [AsParameters] SubscriptionAnalyticsServices services,
        [FromQuery] string userType,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var revenue = await services.AnalyticsService.GetRevenueAsync(userType, startDate, endDate);
            return Results.Ok(new { revenue, userType, startDate, endDate });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving revenue");
            return Results.Problem("An error occurred while retrieving revenue");
        }
    }

    private static async Task<IResult> GetRevenueHistory(
        [AsParameters] SubscriptionAnalyticsServices services,
        [FromQuery] string userType,
        [FromQuery] int months = 12)
    {
        try
        {
            var history = await services.AnalyticsService.GetRevenueHistoryAsync(userType, months);
            return Results.Ok(history);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving revenue history");
            return Results.Problem("An error occurred while retrieving revenue history");
        }
    }

    private static async Task<IResult> GetPredictedRevenue(
        [AsParameters] SubscriptionAnalyticsServices services,
        [FromQuery] string userType,
        [FromQuery] int months = 6)
    {
        try
        {
            var predictions = await services.AnalyticsService.GetPredictedRevenueAsync(userType, months);
            return Results.Ok(predictions);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving predicted revenue");
            return Results.Problem("An error occurred while retrieving predicted revenue");
        }
    }

    private static async Task<IResult> GetSubscriptionsByTier(
        [AsParameters] SubscriptionAnalyticsServices services,
        [FromQuery] string userType)
    {
        try
        {
            var byTier = await services.AnalyticsService.GetSubscriptionsByTierAsync(userType);
            return Results.Ok(byTier);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving subscriptions by tier");
            return Results.Problem("An error occurred while retrieving subscriptions by tier");
        }
    }
}

