using Microsoft.AspNetCore.Mvc;
using ProjectBrain.Domain;

namespace ProjectBrain.Api;

public static class AdminDashboardEndpoints
{
    public static void MapAdminDashboardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("admin/dashboard").RequireAuthorization("AdminOnly");

        group.MapGet("engagement-series", GetEngagementSeries)
            .WithName("GetEngagementSeries")
            .Produces<IReadOnlyList<EngagementSeriesPoint>>();

        group.MapGet("aggregate-usage", GetAggregateUsage)
            .WithName("GetAggregateUsage")
            .Produces<AdminDashboardAggregateResponse>();
    }

    private static async Task<IResult> GetEngagementSeries(
        [FromServices] IStatisticsService statisticsService,
        [FromQuery] string? metric = "conversations",
        [FromQuery] int days = 14)
    {
        if (days < 1 || days > 90)
            return Results.BadRequest("days must be between 1 and 90");

        IReadOnlyList<DailyCountDto> data = metric?.ToLowerInvariant() switch
        {
            "conversations" => await statisticsService.GetConversationsCountByDayAsync(days),
            "quiz-responses" => await statisticsService.GetQuizResponsesCountByDayAsync(days),
            _ => await statisticsService.GetConversationsCountByDayAsync(days)
        };

        var points = data
            .Select(d => new EngagementSeriesPoint(d.Date.ToString("yyyy-MM-dd"), d.Count))
            .ToList();

        return Results.Ok(points);
    }

    private static async Task<IResult> GetAggregateUsage(
        [FromServices] IAdminDashboardUsageService dashboardUsageService,
        CancellationToken cancellationToken)
    {
        var result = await dashboardUsageService.GetAggregateUsageAsync(cancellationToken);
        return Results.Ok(result);
    }
}

public record EngagementSeriesPoint(string Date, int Count);
