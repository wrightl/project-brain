using System.Text.Json;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;
using ProjectBrain.Shared.Dtos.Habits;

public class HabitsServices(
    ILogger<HabitsServices> logger,
    IIdentityService identityService,
    IUserProfileService userProfileService,
    IHabitsCalendarService habitsCalendarService)
{
    public ILogger<HabitsServices> Logger { get; } = logger;
    public IIdentityService IdentityService { get; } = identityService;
    public IUserProfileService UserProfileService { get; } = userProfileService;
    public IHabitsCalendarService HabitsCalendarService { get; } = habitsCalendarService;
}

public static class HabitsEndpoints
{
    public static void MapHabitsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("habits").RequireAuthorization("UserOnly");

        group.MapGet("/yearly-calendar", GetYearlyCalendar).WithName("GetYearlyHabitsCalendar");
    }

    private static async Task<IResult> GetYearlyCalendar([AsParameters] HabitsServices services)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var userProfile = await services.UserProfileService.GetByUserId(userId);
            var timezoneId = TryGetTimezoneId(userProfile?.Preference?.Preferences);

            YearlyHabitsCalendarResponseDto dto = await services.HabitsCalendarService.GetYearlyCalendar(
                userId,
                timezoneId,
                CancellationToken.None);

            return Results.Ok(dto);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error building yearly habits calendar for user {UserId}", userId);
            return Results.Problem("An error occurred while building the yearly habits calendar.");
        }
    }

    private static string? TryGetTimezoneId(string? preferencesJson)
    {
        if (string.IsNullOrWhiteSpace(preferencesJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(preferencesJson);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("timezone", out var tzElement) &&
                tzElement.ValueKind == JsonValueKind.String)
            {
                return tzElement.GetString();
            }
        }
        catch
        {
            // Ignore invalid JSON; fall back to UTC
        }

        return null;
    }
}

