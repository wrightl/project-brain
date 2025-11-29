using Microsoft.AspNetCore.Mvc;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;

public class StatisticsServices(
    ILogger<StatisticsServices> logger,
    IIdentityService identityService,
    IStatisticsService statisticsService)
{
    public ILogger<StatisticsServices> Logger { get; } = logger;
    public IIdentityService IdentityService { get; } = identityService;
    public IStatisticsService StatisticsService { get; } = statisticsService;
}

public static class StatisticsEndpoints
{
    public static void MapStatisticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("statistics").RequireAuthorization();

        group.MapGet("/user-conversations", GetUserConversationsCount).WithName("GetUserConversationsCount");
        group.MapGet("/all-conversations", GetAllConversationsCount).WithName("GetAllConversationsCount");
        group.MapGet("/user-resources", GetUserResourcesCount).WithName("GetUserResourcesCount");
        group.MapGet("/coach-clients", GetCoachClientsCount).WithName("GetCoachClientsCount").RequireAuthorization("CoachOnly");
        group.MapGet("/coach-clients-pending", GetPendingClientsCount).WithName("GetPendingClientsCount").RequireAuthorization("CoachOnly");
        group.MapGet("/shared-resources", GetSharedResourcesCount).WithName("GetSharedResourcesCount").RequireAuthorization("AdminOnly");
        group.MapGet("/all-users", GetAllUsersCount).WithName("GetAllUsersCount").RequireAuthorization("AdminOnly");
        group.MapGet("/coaches", GetCoachesCount).WithName("GetCoachesCount").RequireAuthorization("AdminOnly");
        group.MapGet("/normal-users", GetNormalUsersCount).WithName("GetNormalUsersCount").RequireAuthorization("AdminOnly");
        group.MapGet("/quizzes", GetQuizzesCount).WithName("GetQuizzesCount").RequireAuthorization("AdminOnly");
        group.MapGet("/quiz-responses", GetQuizResponsesCount).WithName("GetQuizResponsesCount").RequireAuthorization("AdminOnly");
        group.MapGet("/logged-in-users", GetLoggedInUsersCount).WithName("GetLoggedInUsersCount").RequireAuthorization("AdminOnly");
        group.MapGet("/conversations", GetConversationsCount).WithName("GetConversationsCount").RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> GetUserConversationsCount(
        [AsParameters] StatisticsServices services,
        string? period = null)
    {
        var userId = services.IdentityService.UserId!;

        try
        {
            var count = await services.StatisticsService.GetUserConversationsCountAsync(userId, period);
            return Results.Ok(new { count, period });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving user conversations count for user {UserId}", userId);
            return Results.Problem("An error occurred while retrieving user conversations count.");
        }
    }

    private static async Task<IResult> GetAllConversationsCount(
        [AsParameters] StatisticsServices services,
        string? period = null)
    {
        try
        {
            var count = await services.StatisticsService.GetAllConversationsCountAsync(period);
            return Results.Ok(new { count, period });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving all conversations count");
            return Results.Problem("An error occurred while retrieving all conversations count.");
        }
    }

    private static async Task<IResult> GetUserResourcesCount([AsParameters] StatisticsServices services)
    {
        var userId = services.IdentityService.UserId!;

        try
        {
            var count = await services.StatisticsService.GetUserResourcesCountAsync(userId);
            return Results.Ok(new { count });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving user resources count for user {UserId}", userId);
            return Results.Problem("An error occurred while retrieving user resources count.");
        }
    }

    private static async Task<IResult> GetCoachClientsCount([AsParameters] StatisticsServices services)
    {
        var userId = services.IdentityService.UserId!;

        try
        {
            var count = await services.StatisticsService.GetCoachClientsCountAsync(userId);
            return Results.Ok(new { count });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving coach clients count for user {UserId}", userId);
            return Results.Problem("An error occurred while retrieving coach clients count.");
        }
    }

    private static async Task<IResult> GetPendingClientsCount([AsParameters] StatisticsServices services)
    {
        var userId = services.IdentityService.UserId!;
        try
        {
            var count = await services.StatisticsService.GetPendingClientsCountAsync(userId);
            return Results.Ok(new { count });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving pending clients count for user {UserId}", userId);
            return Results.Problem("An error occurred while retrieving pending clients count.");
        }
    }

    private static async Task<IResult> GetSharedResourcesCount([AsParameters] StatisticsServices services)
    {
        try
        {
            var count = await services.StatisticsService.GetSharedResourcesCountAsync();
            return Results.Ok(new { count });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving shared resources count");
            return Results.Problem("An error occurred while retrieving shared resources count.");
        }
    }

    private static async Task<IResult> GetAllUsersCount([AsParameters] StatisticsServices services)
    {
        try
        {
            var count = await services.StatisticsService.GetAllUsersCountAsync();
            return Results.Ok(new { count });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving all users count");
            return Results.Problem("An error occurred while retrieving all users count.");
        }
    }

    private static async Task<IResult> GetCoachesCount([AsParameters] StatisticsServices services)
    {
        try
        {
            var count = await services.StatisticsService.GetCoachesCountAsync();
            return Results.Ok(new { count });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving coaches count");
            return Results.Problem("An error occurred while retrieving coaches count.");
        }
    }

    private static async Task<IResult> GetNormalUsersCount([AsParameters] StatisticsServices services)
    {
        try
        {
            var count = await services.StatisticsService.GetNormalUsersCountAsync();
            return Results.Ok(new { count });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving normal users count");
            return Results.Problem("An error occurred while retrieving normal users count.");
        }
    }

    private static async Task<IResult> GetQuizzesCount([AsParameters] StatisticsServices services)
    {
        try
        {
            var count = await services.StatisticsService.GetQuizzesCountAsync();
            return Results.Ok(new { count });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving quizzes count");
            return Results.Problem("An error occurred while retrieving quizzes count.");
        }
    }

    private static async Task<IResult> GetQuizResponsesCount(
        [AsParameters] StatisticsServices services,
        string? period = null)
    {
        services.Logger.LogInformation("Getting quiz responses count with period {Period}", period);

        try
        {
            var count = await services.StatisticsService.GetQuizResponsesCountAsync(period);
            return Results.Ok(new { count, period });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving quiz responses count");
            return Results.Problem("An error occurred while retrieving quiz responses count.");
        }
    }

    private static async Task<IResult> GetLoggedInUsersCount([AsParameters] StatisticsServices services)
    {
        try
        {
            var count = await services.StatisticsService.GetLoggedInUsersCountAsync();
            return Results.Ok(new { count });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving logged in users count");
            return Results.Problem("An error occurred while retrieving logged in users count.");
        }
    }

    private static async Task<IResult> GetConversationsCount(
        [AsParameters] StatisticsServices services,
        string? period = null)
    {
        try
        {
            var count = await services.StatisticsService.GetConversationsCountAsync(period);
            return Results.Ok(new { count, period });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving conversations count");
            return Results.Problem("An error occurred while retrieving conversations count.");
        }
    }
}

