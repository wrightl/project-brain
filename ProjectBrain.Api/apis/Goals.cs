using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Api.Background;
using ProjectBrain.AI;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Mappers;
using ProjectBrain.Shared.Dtos.Goals;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

public class GoalServices(
    ILogger<GoalServices> logger,
    IGoalService goalService,
    IIdentityService identityService,
    IGoalsUpdatedBroadcaster goalsUpdatedBroadcaster,
    IPushNotificationService pushNotificationService,
    ITimeTickerManager<TimeTickerEntity> timeTickerManager)
{
    public ILogger<GoalServices> Logger { get; } = logger;
    public IGoalService GoalService { get; } = goalService;
    public IIdentityService IdentityService { get; } = identityService;
    public IGoalsUpdatedBroadcaster GoalsUpdatedBroadcaster { get; } = goalsUpdatedBroadcaster;
    public IPushNotificationService PushNotificationService { get; } = pushNotificationService;
    public ITimeTickerManager<TimeTickerEntity> TimeTickerManager { get; } = timeTickerManager;
}

public static class GoalEndpoints
{
    public static void MapGoalEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("eggs").RequireAuthorization("UserOnly");

        group.MapGet("/", GetTodaysGoals).WithName("GetTodaysGoals");
        group.MapGet("/stream", StreamGoals).WithName("StreamGoals");
        group.MapPost("/", CreateOrUpdateGoals).WithName("CreateOrUpdateGoals");
        group.MapPost("/{index}/complete", CompleteGoal).WithName("CompleteGoal");
        group.MapGet("/streak", GetCompletionStreak).WithName("GetCompletionStreak");
        group.MapGet("/streak-summary", GetStreakSummary).WithName("GetStreakSummary");
        group.MapGet("/has-ever-created", HasEverCreatedGoals).WithName("HasEverCreatedGoals");
    }

    private static async Task<IResult> StreamGoals(
        [AsParameters] GoalServices services,
        HttpContext http)
    {
        var currentUserId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(currentUserId))
            return Results.Unauthorized();

        http.Response.ContentType = "text/event-stream";
        http.Response.Headers.CacheControl = "no-cache";
        http.Response.Headers.Connection = "keep-alive";
        await http.Response.StartAsync(http.RequestAborted);

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        try
        {
            await services.GoalsUpdatedBroadcaster.SubscribeAsync(
                currentUserId,
                async (evt, ct) =>
                {
                    await http.Response.WriteAsync("event: goals-updated\n", ct);
                    var data = JsonSerializer.Serialize(new { updatedAt = evt.UpdatedAt }, options);
                    await http.Response.WriteAsync($"data: {data}\n\n", ct);
                    await http.Response.Body.FlushAsync(ct);
                },
                http.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected
        }

        return Results.Empty;
    }

    private static void NotifyGoalsUpdatedAndPush(GoalServices services, string userId)
    {
        var evt = new GoalsUpdatedEvent { UpdatedAt = DateTime.UtcNow.ToString("O") };
        services.GoalsUpdatedBroadcaster.NotifyGoalsUpdated(userId, evt);

        _ = Task.Run(async () =>
        {
            try
            {
                await services.PushNotificationService.SendDataOnlyToUserAsync(
                    userId,
                    new Dictionary<string, string> { ["type"] = "goals_updated" });
            }
            catch (Exception ex)
            {
                services.Logger.LogWarning(ex, "Failed to send goals_updated FCM to user {UserId}", userId);
            }
        });
    }

    private static async Task<IResult> GetTodaysGoals(
        [AsParameters] GoalServices services)
    {
        var currentUserId = services.IdentityService.UserId!;

        try
        {
            var goals = await services.GoalService.GetTodaysGoalsAsync(currentUserId);
            var response = GoalMapper.ToDtoList(goals).ToList();

            // Ensure we always return exactly 3 goals
            while (response.Count < 3)
            {
                response.Add(new GoalResponseDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Index = response.Count,
                    Message = string.Empty,
                    Completed = false,
                    CompletedAt = null,
                    CreatedAt = DateTime.UtcNow.ToString("O"),
                    UpdatedAt = DateTime.UtcNow.ToString("O")
                });
            }

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error getting today's goals for user {UserId}", currentUserId);
            return Results.Problem("An error occurred while fetching goals.");
        }
    }

    private static async Task<IResult> CreateOrUpdateGoals(
        [AsParameters] GoalServices services,
        [FromBody] CreateOrUpdateGoalsRequestDto request)
    {
        var currentUserId = services.IdentityService.UserId!;

        try
        {
            var goals = await services.GoalService.CreateOrUpdateGoalsAsync(
                currentUserId,
                request.Goals,
                CancellationToken.None);

            var response = GoalMapper.ToDtoList(goals).ToList();

            NotifyGoalsUpdatedAndPush(services, currentUserId);

            await UserContextTickerEnqueue.EnqueueGoalsUploadAsync(services.TimeTickerManager, currentUserId);

            // Check if goals existed before (by checking if any had non-empty messages)
            // Since we just created/updated, we need to check if this was the first time
            // We'll use a simple heuristic: if all goals are new (just created), return 201
            // Otherwise return 200. For simplicity, we'll always return 200 since the service
            // handles both create and update the same way.
            return Results.Ok(response);
        }
        catch (ArgumentException ex)
        {
            services.Logger.LogWarning(ex, "Invalid request for user {UserId}", currentUserId);
            return Results.BadRequest(new
            {
                error = new
                {
                    code = "INVALID_REQUEST",
                    message = ex.Message
                }
            });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error creating/updating goals for user {UserId}", currentUserId);
            return Results.Problem("An error occurred while saving goals.");
        }
    }

    private static async Task<IResult> CompleteGoal(
        [AsParameters] GoalServices services,
        int index,
        [FromBody] CompleteGoalRequestDto request)
    {
        var currentUserId = services.IdentityService.UserId!;

        try
        {
            if (index < 0 || index > 2)
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        code = "INVALID_INDEX",
                        message = "Goal index must be 0, 1, or 2"
                    }
                });
            }

            services.Logger.LogInformation("Completing goal for user {UserId} at index {Index} with completed status {Completed}", currentUserId, index, request.Completed);

            var goals = await services.GoalService.CompleteGoalAsync(
                currentUserId,
                index,
                request.Completed,
                CancellationToken.None);

            var response = GoalMapper.ToDtoList(goals).ToList();

            NotifyGoalsUpdatedAndPush(services, currentUserId);

            await UserContextTickerEnqueue.EnqueueGoalsUploadAsync(services.TimeTickerManager, currentUserId);

            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            services.Logger.LogWarning(ex, "Goal not found for user {UserId} at index {Index}", currentUserId, index);
            return Results.NotFound(new
            {
                error = new
                {
                    code = "GOAL_NOT_FOUND",
                    message = ex.Message
                }
            });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error completing goal for user {UserId} at index {Index}", currentUserId, index);
            return Results.Problem("An error occurred while updating the goal.");
        }
    }

    private static async Task<IResult> GetCompletionStreak(
        [AsParameters] GoalServices services)
    {
        var currentUserId = services.IdentityService.UserId!;

        try
        {
            var streak = await services.GoalService.GetCompletionStreakAsync(currentUserId);
            return Results.Ok(new { streak });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error getting completion streak for user {UserId}", currentUserId);
            return Results.Problem("An error occurred while fetching streak.");
        }
    }

    private static async Task<IResult> GetStreakSummary(
        [AsParameters] GoalServices services)
    {
        var currentUserId = services.IdentityService.UserId!;

        try
        {
            var current = await services.GoalService.GetCompletionStreakAsync(currentUserId);
            var longest = await services.GoalService.GetLongestCompletionStreakAsync(currentUserId);

            return Results.Ok(new StreakSummaryResponseDto
            {
                CurrentStreak = current,
                LongestStreak = longest
            });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error getting streak summary for user {UserId}", currentUserId);
            return Results.Problem("An error occurred while fetching streak summary.");
        }
    }

    private static async Task<IResult> HasEverCreatedGoals(
        [AsParameters] GoalServices services)
    {
        var currentUserId = services.IdentityService.UserId!;

        try
        {
            var hasEverCreated = await services.GoalService.HasEverCreatedGoalsAsync(currentUserId);
            return Results.Ok(new { hasEverCreated });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error checking if user has ever created goals for user {UserId}", currentUserId);
            return Results.Problem("An error occurred while checking goal history.");
        }
    }

}
