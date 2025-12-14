using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain.Exceptions;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Shared.Dtos.Pagination;

public class ConnectionServices(
    ILogger<ConnectionServices> logger,
    IConnectionService connectionService,
    IConnectionRepository connectionRepository,
    IIdentityService identityService,
    IUserProfileService userProfileService,
    ICoachProfileService coachProfileService)
{
    public ILogger<ConnectionServices> Logger { get; } = logger;
    public IConnectionService ConnectionService { get; } = connectionService;
    public IConnectionRepository ConnectionRepository { get; } = connectionRepository;
    public IIdentityService IdentityService { get; } = identityService;
    public IUserProfileService UserProfileService { get; } = userProfileService;
    public ICoachProfileService CoachProfileService { get; } = coachProfileService;
}

public static class ConnectionEndpoints
{
    public static void MapConnectionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("connections").RequireAuthorization();
        group.MapGet("/{connectionId:guid}", GetConnectionById).WithName("GetConnectionById");
        group.MapGet("", GetAllConnections).WithName("GetAllConnections");
        group.MapDelete("/{connectionId:guid}", DeleteConnection).WithName("DeleteConnection");
    }

    private static async Task<IResult> GetConnectionById(
        [AsParameters] ConnectionServices services,
        Guid connectionId)
    {
        var currentUserId = services.IdentityService.UserId!;

        var connection = await services.ConnectionService.GetByIdAsync(connectionId);

        if (connection == null)
        {
            return Results.NotFound(new
            {
                error = new
                {
                    code = "CONNECTION_NOT_FOUND",
                    message = "Connection not found"
                }
            });
        }

        // Verify user has access to this connection
        if (currentUserId != connection.UserId && currentUserId != connection.CoachId)
        {
            return Results.Forbid();
        }

        // Return the user & coach profile id, instead of their internal ids
        var userProfile = await services.UserProfileService.GetByUserId(connection.UserId);
        var coachProfile = await services.CoachProfileService.GetByUserId(connection.CoachId);

        return Results.Ok(new
        {
            id = connection.Id.ToString(),
            userProfileId = userProfile?.Id,
            coachProfileId = coachProfile?.Id,
            status = connection.Status,
            requestedAt = connection.RequestedAt,
            respondedAt = connection.RespondedAt,
            requestedBy = connection.RequestedBy,
            message = connection.Message
        });
    }

    private static async Task<IResult> GetAllConnections(
        [AsParameters] ConnectionServices services,
        HttpRequest request)
    {
        var currentUserId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(currentUserId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        var isCoach = services.IdentityService.IsCoach;

        // Parse pagination parameters
        var pagedRequest = new PagedRequest();
        if (request.Query.TryGetValue("page", out var pageValue) &&
            int.TryParse(pageValue, out var page) && page > 0)
        {
            pagedRequest.Page = page;
        }

        if (request.Query.TryGetValue("pageSize", out var pageSizeValue) &&
            int.TryParse(pageSizeValue, out var pageSize) && pageSize > 0)
        {
            pagedRequest.PageSize = pageSize;
        }

        // Get total count for pagination
        var totalCount = await services.ConnectionRepository.CountConnectionsAsync(currentUserId, isCoach, CancellationToken.None);

        // Get paginated results using efficient database-level pagination
        var skip = pagedRequest.GetSkip();
        var take = pagedRequest.GetTake();
        var paginatedConnections = await services.ConnectionRepository.GetPagedConnectionsAsync(currentUserId, isCoach, skip, take, CancellationToken.None);

        // Map to ConnectionWithStatus and enrich with coachProfileId
        var connectionWithStatusList = new List<ConnectionWithStatus>();
        foreach (var connection in paginatedConnections)
        {
            var connectionWithStatus = ConnectionWithStatus.FromConnection(connection);

            // Get coachProfileId if this is a user viewing their coaches
            if (!isCoach && connection.Coach != null)
            {
                var coachProfile = await services.CoachProfileService.GetByUserId(connection.CoachId);
                if (coachProfile != null)
                {
                    connectionWithStatus = new ConnectionWithStatus
                    {
                        Id = connectionWithStatus.Id,
                        UserId = connectionWithStatus.UserId,
                        CoachId = connectionWithStatus.CoachId,
                        Status = connectionWithStatus.Status,
                        UserName = connectionWithStatus.UserName,
                        CoachName = connectionWithStatus.CoachName,
                        CoachProfileId = coachProfile.Id.ToString(),
                        RequestedAt = connectionWithStatus.RequestedAt,
                        RespondedAt = connectionWithStatus.RespondedAt
                    };
                }
            }

            connectionWithStatusList.Add(connectionWithStatus);
        }

        var response = PagedResponse<ConnectionWithStatus>.Create(pagedRequest, connectionWithStatusList, totalCount);
        return Results.Ok(response);
    }

    private static async Task<IResult> DeleteConnection(
        [AsParameters] ConnectionServices services,
        Guid connectionId)
    {
        var currentUserId = services.IdentityService.UserId!;

        var connection = await services.ConnectionService.GetByIdAsync(connectionId);

        if (connection == null)
        {
            return Results.NotFound(new
            {
                error = new
                {
                    code = "CONNECTION_NOT_FOUND",
                    message = "Connection not found"
                }
            });
        }

        // Verify user has access to this connection
        if (currentUserId != connection.UserId && currentUserId != connection.CoachId)
        {
            return Results.Forbid();
        }

        var success = await services.ConnectionService.CancelOrDeleteConnectionAsync(connectionId);

        if (!success)
        {
            return Results.BadRequest(new
            {
                error = new
                {
                    code = "DELETE_FAILED",
                    message = "Failed to delete connection"
                }
            });
        }

        return Results.NoContent();
    }
}