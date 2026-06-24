using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Dtos;

public class UserDataExportServices(
    ILogger<UserDataExportServices> logger,
    IIdentityService identityService,
    IUserDataExportService userDataExportService)
{
    public ILogger<UserDataExportServices> Logger { get; } = logger;
    public IIdentityService IdentityService { get; } = identityService;
    public IUserDataExportService UserDataExportService { get; } = userDataExportService;
}

public static class UserDataExportEndpoints
{
    public static void MapUserDataExportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("user/data-export").RequireAuthorization("UserOnly");
        group.MapGet("", ExportUserData).WithName("ExportUserData");
    }

    private static async Task<IResult> ExportUserData([AsParameters] UserDataExportServices services)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var export = await services.UserDataExportService.ExportUserDataAsync(userId);
            return Results.Json(export);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error exporting data for user {UserId}", userId);
            return Results.Problem("An error occurred while exporting your data.");
        }
    }
}
