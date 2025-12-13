using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;

public class ConnectionServices(
    ILogger<ConnectionServices> logger,
    IConnectionService connectionService,
    IIdentityService identityService,
    IUserProfileService userProfileService,
    ICoachProfileService coachProfileService)
{
    public ILogger<ConnectionServices> Logger { get; } = logger;
    public IConnectionService ConnectionService { get; } = connectionService;
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
        [AsParameters] ConnectionServices services)
    {
        var currentUserId = services.IdentityService.UserId!;
        var isCoach = services.IdentityService.IsCoach;

        var connections = await services.ConnectionService.GetConnectionsAsync(currentUserId, isCoach);

        return Results.Ok(connections);
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