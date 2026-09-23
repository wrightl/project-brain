using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Exceptions;
using ProjectBrain.Shared.Constants;
using ProjectBrain.Shared.Dtos.Community;

public class CommunityServices(
    ICommunityService communityService,
    IFeatureFlagService featureFlagService,
    IIdentityService identityService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<CommunityServices> logger)
{
    public ICommunityService CommunityService { get; } = communityService;
    public IFeatureFlagService FeatureFlagService { get; } = featureFlagService;
    public IIdentityService IdentityService { get; } = identityService;
    public IHttpContextAccessor HttpContextAccessor { get; } = httpContextAccessor;
    public ILogger<CommunityServices> Logger { get; } = logger;
}

public static class CommunityEndpoints
{
    public static void MapCommunityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("community").RequireAuthorization();

        group.MapGet("/channels", GetChannels).WithName("GetCommunityChannels");
        group.MapGet("/feed", GetLatestFeed).WithName("GetCommunityLatestFeed");
        group.MapGet("/channels/{slug}/posts", GetPosts).WithName("GetCommunityPosts");
        group.MapPost("/channels/{slug}/posts", CreatePost).WithName("CreateCommunityPost");
        group.MapDelete("/posts/{id:guid}", DeletePost).WithName("DeleteCommunityPost");
        group.MapPost("/posts/{id:guid}/reactions", AddReaction).WithName("AddCommunityReaction");
        group.MapDelete("/posts/{id:guid}/reactions", RemoveReaction).WithName("RemoveCommunityReaction");
        group.MapPost("/posts/{id:guid}/reports", CreateReport).WithName("CreateCommunityReport");

        group.MapGet("/admin/reports", GetOpenReports).WithName("GetCommunityReports");
        group.MapPost("/admin/reports/{id:guid}/resolve", ResolveReport).WithName("ResolveCommunityReport");
        group.MapPost("/posts/{id:guid}/hide", HidePost).WithName("HideCommunityPost");
        group.MapPatch("/channels/{id:guid}", UpdateChannel).WithName("UpdateCommunityChannel");
    }

    private static async Task EnsureCommunityEnabledAsync(CommunityServices services)
    {
        var enabled = await services.FeatureFlagService.IsFeatureEnabled(FeatureFlags.CommunityFeatureEnabled);
        if (!enabled)
        {
            throw new AppException("NOT_FOUND", "Community is not available", 404);
        }
    }

    private static string RequireUserId(CommunityServices services)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        return userId;
    }

    private static void RequireAppUserRole(CommunityServices services)
    {
        var user = services.HttpContextAccessor.HttpContext?.User;
        if (user == null || !user.HasAppRole(AppRoles.User))
        {
            throw new AppException("FORBIDDEN", "Community is only available to users", 403);
        }
    }

    private static void RequireAdmin(CommunityServices services)
    {
        if (!services.IdentityService.IsAdmin)
        {
            throw new AppException("FORBIDDEN", "Admin access required", 403);
        }
    }

    private static async Task<IResult> GetChannels([AsParameters] CommunityServices services)
    {
        await EnsureCommunityEnabledAsync(services);
        RequireAppUserRole(services);
        RequireUserId(services);

        var channels = await services.CommunityService.GetChannelsAsync();
        return Results.Ok(channels);
    }

    private static async Task<IResult> GetLatestFeed(
        [AsParameters] CommunityServices services,
        string? cursor,
        int? limit)
    {
        await EnsureCommunityEnabledAsync(services);
        RequireAppUserRole(services);
        var userId = RequireUserId(services);

        var page = await services.CommunityService.GetLatestFeedAsync(
            cursor,
            limit ?? 20,
            userId);
        return Results.Ok(page);
    }

    private static async Task<IResult> GetPosts(
        [AsParameters] CommunityServices services,
        string slug,
        string? cursor,
        int? limit)
    {
        await EnsureCommunityEnabledAsync(services);
        RequireAppUserRole(services);
        var userId = RequireUserId(services);

        var page = await services.CommunityService.GetPostsAsync(
            slug,
            cursor,
            limit ?? 20,
            userId);
        return Results.Ok(page);
    }

    private static async Task<IResult> CreatePost(
        [AsParameters] CommunityServices services,
        string slug,
        CreateCommunityPostRequestDto request)
    {
        await EnsureCommunityEnabledAsync(services);
        RequireAppUserRole(services);
        var userId = RequireUserId(services);

        var post = await services.CommunityService.CreatePostAsync(slug, userId, request.Body);
        return Results.Created($"/community/posts/{post.Id}", post);
    }

    private static async Task<IResult> DeletePost(
        [AsParameters] CommunityServices services,
        Guid id)
    {
        await EnsureCommunityEnabledAsync(services);
        RequireAppUserRole(services);
        var userId = RequireUserId(services);

        await services.CommunityService.DeleteOwnPostAsync(id, userId);
        return Results.NoContent();
    }

    private static async Task<IResult> AddReaction(
        [AsParameters] CommunityServices services,
        Guid id)
    {
        await EnsureCommunityEnabledAsync(services);
        RequireAppUserRole(services);
        var userId = RequireUserId(services);

        await services.CommunityService.AddReactionAsync(id, userId);
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveReaction(
        [AsParameters] CommunityServices services,
        Guid id)
    {
        await EnsureCommunityEnabledAsync(services);
        RequireAppUserRole(services);
        var userId = RequireUserId(services);

        await services.CommunityService.RemoveReactionAsync(id, userId);
        return Results.NoContent();
    }

    private static async Task<IResult> CreateReport(
        [AsParameters] CommunityServices services,
        Guid id,
        CreateCommunityReportRequestDto request)
    {
        await EnsureCommunityEnabledAsync(services);
        RequireAppUserRole(services);
        var userId = RequireUserId(services);

        await services.CommunityService.CreateReportAsync(id, userId, request.Reason);
        return Results.NoContent();
    }

    private static async Task<IResult> GetOpenReports([AsParameters] CommunityServices services)
    {
        RequireAdmin(services);
        var reports = await services.CommunityService.GetOpenReportsAsync();
        return Results.Ok(reports);
    }

    private static async Task<IResult> ResolveReport(
        [AsParameters] CommunityServices services,
        Guid id)
    {
        RequireAdmin(services);
        await services.CommunityService.ResolveReportAsync(id);
        return Results.NoContent();
    }

    private static async Task<IResult> HidePost(
        [AsParameters] CommunityServices services,
        Guid id)
    {
        RequireAdmin(services);
        var adminId = RequireUserId(services);
        await services.CommunityService.HidePostAsync(id, adminId);
        return Results.NoContent();
    }

    private static async Task<IResult> UpdateChannel(
        [AsParameters] CommunityServices services,
        Guid id,
        UpdateCommunityChannelRequestDto request)
    {
        RequireAdmin(services);
        var channel = await services.CommunityService.UpdateChannelAsync(id, request);
        return Results.Ok(channel);
    }
}
