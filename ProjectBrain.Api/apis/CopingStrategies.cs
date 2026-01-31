using Microsoft.AspNetCore.Mvc;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;
using ProjectBrain.Shared.Dtos.CopingStrategies;

public class CopingStrategyServices(
    ILogger<CopingStrategyServices> logger,
    ICopingStrategyService copingStrategyService,
    IIdentityService identityService)
{
    public ILogger<CopingStrategyServices> Logger { get; } = logger;
    public ICopingStrategyService CopingStrategyService { get; } = copingStrategyService;
    public IIdentityService IdentityService { get; } = identityService;
}

public static class CopingStrategyEndpoints
{
    public static void MapCopingStrategyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("coping-strategies").RequireAuthorization("UserOnly");

        group.MapGet("/library", GetLibrary).WithName("GetCopingStrategyLibrary");
        group.MapPost("", Create).WithName("CreateCopingStrategy");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteCopingStrategy");
    }

    private static async Task<IResult> GetLibrary([AsParameters] CopingStrategyServices services)
    {
        var userId = services.IdentityService.UserId!;

        try
        {
            var library = await services.CopingStrategyService.GetLibraryAsync(userId);

            var items = library
                .Select(x => new CopingStrategyLibraryItemDto
                {
                    Id = x.Id.ToString(),
                    Title = x.Title,
                    Description = x.Description,
                    IconKey = x.IconKey,
                    SavedAt = x.SavedAt
                })
                .ToList();

            return Results.Ok(new CopingStrategyLibraryResponseDto { Items = items });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error getting coping strategy library for user {UserId}", userId);
            return Results.Problem("An error occurred while fetching coping strategy library.");
        }
    }

    private static async Task<IResult> Create(
        [AsParameters] CopingStrategyServices services,
        [FromBody] CreateCopingStrategyRequest request)
    {
        var userId = services.IdentityService.UserId!;

        try
        {
            var created = await services.CopingStrategyService.CreateAsync(
                userId,
                request.Title,
                request.Description,
                request.IconKey,
                CancellationToken.None);

            return Results.Ok(new CopingStrategyLibraryItemDto
            {
                Id = created.Id.ToString(),
                Title = created.Title,
                Description = created.Description,
                IconKey = created.IconKey,
                SavedAt = created.SavedAt
            });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error creating coping strategy for user {UserId}", userId);
            return Results.Problem("An error occurred while creating coping strategy.");
        }
    }

    private static async Task<IResult> Delete(
        [AsParameters] CopingStrategyServices services,
        Guid id)
    {
        var userId = services.IdentityService.UserId!;

        try
        {
            var deleted = await services.CopingStrategyService.DeleteAsync(userId, id, CancellationToken.None);
            return deleted ? Results.NoContent() : Results.NotFound();
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error deleting coping strategy {StrategyId} for user {UserId}", id, userId);
            return Results.Problem("An error occurred while deleting coping strategy.");
        }
    }
}

public class CreateCopingStrategyRequest
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public string? IconKey { get; init; }
}

