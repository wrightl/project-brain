using Microsoft.AspNetCore.Mvc;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Mappers;
using ProjectBrain.Shared.Dtos.Goals;

public class GoalServices(
    ILogger<GoalServices> logger,
    IGoalService goalService,
    IIdentityService identityService)
{
    public ILogger<GoalServices> Logger { get; } = logger;
    public IGoalService GoalService { get; } = goalService;
    public IIdentityService IdentityService { get; } = identityService;
}

public static class GoalEndpoints
{
    public static void MapGoalEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("eggs").RequireAuthorization("UserOnly");

        group.MapGet("/", GetTodaysGoals).WithName("GetTodaysGoals");
        group.MapPost("/", CreateOrUpdateGoals).WithName("CreateOrUpdateGoals");
        group.MapPost("/{index}/complete", CompleteGoal).WithName("CompleteGoal");
        group.MapGet("/streak", GetCompletionStreak).WithName("GetCompletionStreak");
        group.MapGet("/has-ever-created", HasEverCreatedGoals).WithName("HasEverCreatedGoals");
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

            var goals = await services.GoalService.CompleteGoalAsync(
                currentUserId,
                index,
                request.Completed,
                CancellationToken.None);

            var response = GoalMapper.ToDtoList(goals).ToList();

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
