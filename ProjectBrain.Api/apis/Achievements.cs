using Microsoft.AspNetCore.Mvc;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;
using ProjectBrain.Shared.Dtos.Achievements;

public class AchievementServices(
    ILogger<AchievementServices> logger,
    IAchievementService achievementService,
    IIdentityService identityService)
{
    public ILogger<AchievementServices> Logger { get; } = logger;
    public IAchievementService AchievementService { get; } = achievementService;
    public IIdentityService IdentityService { get; } = identityService;
}

public static class AchievementEndpoints
{
    public static void MapAchievementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("achievements").RequireAuthorization("UserOnly");
        group.MapGet("", GetUserAchievements).WithName("GetUserAchievements");
    }

    private static async Task<IResult> GetUserAchievements([AsParameters] AchievementServices services)
    {
        var userId = services.IdentityService.UserId!;

        try
        {
            var earned = await services.AchievementService.GetEarnedForUserAsync(userId, CancellationToken.None);

            var items = earned
                .Where(x => x.Achievement != null)
                .Select(x => new AchievementItemDto
                {
                    Id = x.Achievement!.Id.ToString(),
                    Key = x.Achievement!.Key,
                    Title = x.Achievement!.Title,
                    Description = x.Achievement!.Description,
                    IconKey = x.Achievement!.IconKey,
                    EarnedAt = x.EarnedAt
                })
                .ToList();

            return Results.Ok(new AchievementsResponseDto { Items = items });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error getting achievements for user {UserId}", userId);
            return Results.Problem("An error occurred while fetching achievements.");
        }
    }
}

