using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;

public class UserMemoryServices(
    ILogger<UserMemoryServices> logger,
    IIdentityService identityService,
    IUserMemoryService userMemoryService)
{
    public ILogger<UserMemoryServices> Logger { get; } = logger;
    public IIdentityService IdentityService { get; } = identityService;
    public IUserMemoryService UserMemoryService { get; } = userMemoryService;
}

public static class UserMemoryEndpoints
{
    public static void MapUserMemoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("user/memory").RequireAuthorization("UserOnly");

        group.MapGet("", ListMemories).WithName("ListUserMemories");
        group.MapDelete("/facts/{id:guid}", DeleteFact).WithName("DeleteUserFact");
        group.MapDelete("/episodes/{id:guid}", DeleteEpisode).WithName("DeleteUserEpisode");
        group.MapPost("/facts/{id:guid}/pin", PinFact).WithName("PinUserFact");
        group.MapPost("/facts/{id:guid}/unpin", UnpinFact).WithName("UnpinUserFact");
        group.MapPost("/episodes/{id:guid}/pin", PinEpisode).WithName("PinUserEpisode");
        group.MapPost("/episodes/{id:guid}/unpin", UnpinEpisode).WithName("UnpinUserEpisode");
    }

    private static async Task<IResult> ListMemories([AsParameters] UserMemoryServices services)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var memories = await services.UserMemoryService.ListAsync(userId);
            return Results.Ok(memories);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error listing memories for user {UserId}", userId);
            return Results.Problem("An error occurred while fetching learned memories.");
        }
    }

    private static async Task<IResult> DeleteFact(
        [AsParameters] UserMemoryServices services,
        Guid id)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var deleted = await services.UserMemoryService.DeleteFactAsync(userId, id);
            return deleted ? Results.NoContent() : Results.NotFound();
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error deleting fact {FactId} for user {UserId}", id, userId);
            return Results.Problem("An error occurred while deleting the memory.");
        }
    }

    private static async Task<IResult> DeleteEpisode(
        [AsParameters] UserMemoryServices services,
        Guid id)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var deleted = await services.UserMemoryService.DeleteEpisodeAsync(userId, id);
            return deleted ? Results.NoContent() : Results.NotFound();
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error deleting episode {EpisodeId} for user {UserId}", id, userId);
            return Results.Problem("An error occurred while deleting the memory.");
        }
    }

    private static async Task<IResult> PinFact(
        [AsParameters] UserMemoryServices services,
        Guid id)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var pinned = await services.UserMemoryService.PinFactAsync(userId, id);
            return pinned ? Results.NoContent() : Results.NotFound();
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error pinning fact {FactId} for user {UserId}", id, userId);
            return Results.Problem("An error occurred while pinning the memory.");
        }
    }

    private static async Task<IResult> UnpinFact(
        [AsParameters] UserMemoryServices services,
        Guid id)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var unpinned = await services.UserMemoryService.UnpinFactAsync(userId, id);
            return unpinned ? Results.NoContent() : Results.NotFound();
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error unpinning fact {FactId} for user {UserId}", id, userId);
            return Results.Problem("An error occurred while unpinning the memory.");
        }
    }

    private static async Task<IResult> PinEpisode(
        [AsParameters] UserMemoryServices services,
        Guid id)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var pinned = await services.UserMemoryService.PinEpisodeAsync(userId, id);
            return pinned ? Results.NoContent() : Results.NotFound();
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error pinning episode {EpisodeId} for user {UserId}", id, userId);
            return Results.Problem("An error occurred while pinning the memory.");
        }
    }

    private static async Task<IResult> UnpinEpisode(
        [AsParameters] UserMemoryServices services,
        Guid id)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var unpinned = await services.UserMemoryService.UnpinEpisodeAsync(userId, id);
            return unpinned ? Results.NoContent() : Results.NotFound();
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error unpinning episode {EpisodeId} for user {UserId}", id, userId);
            return Results.Problem("An error occurred while unpinning the memory.");
        }
    }
}
