using ProjectBrain.Domain;

namespace ProjectBrain.Api;

public static class AdminActivityEndpoints
{
    public static void MapAdminActivityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("admin/activity").RequireAuthorization("AdminOnly");

        group.MapPost("sync", TriggerActivitySync)
            .WithName("TriggerActivitySync")
            .Produces(StatusCodes.Status202Accepted);
    }

    private static IResult TriggerActivitySync(IUserActivitySyncTrigger syncTrigger)
    {
        syncTrigger.RequestSync();
        return Results.Accepted();
    }
}
