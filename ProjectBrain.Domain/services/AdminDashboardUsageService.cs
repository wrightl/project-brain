namespace ProjectBrain.Domain;

public interface IAdminDashboardUsageService
{
    Task<AdminDashboardAggregateResponse> GetAggregateUsageAsync(CancellationToken cancellationToken = default);
}

public class AdminDashboardUsageService : IAdminDashboardUsageService
{
    private readonly IStatisticsService _statisticsService;
    private readonly IUsageTrackingService _usageTrackingService;

    public AdminDashboardUsageService(
        IStatisticsService statisticsService,
        IUsageTrackingService usageTrackingService)
    {
        _statisticsService = statisticsService;
        _usageTrackingService = usageTrackingService;
    }

    public async Task<AdminDashboardAggregateResponse> GetAggregateUsageAsync(CancellationToken cancellationToken = default)
    {
        // Run sequentially to avoid DbContext concurrent use (StatisticsService and UsageTrackingService share the same scoped DbContext).
        var totalUsers = await _statisticsService.GetAllUsersCountAsync();
        var totalCoaches = await _statisticsService.GetCoachesCountAsync();
        var normalUsers = await _statisticsService.GetNormalUsersCountAsync();
        var loggedInUsers = await _statisticsService.GetLoggedInUsersCountAsync();
        var totalAiQueriesDaily = await _usageTrackingService.GetTotalUsageCountAsync("ai_query", "daily");
        var totalAiQueriesMonthly = await _usageTrackingService.GetTotalUsageCountAsync("ai_query", "monthly");
        var totalStorageBytes = await _usageTrackingService.GetTotalFileStorageBytesAsync();
        var totalStorageMb = totalStorageBytes / (1024.0 * 1024.0);

        return new AdminDashboardAggregateResponse(
            TotalUsers: totalUsers,
            TotalCoaches: totalCoaches,
            NormalUsers: normalUsers,
            LoggedInUsers: loggedInUsers,
            TotalAiQueriesDaily: totalAiQueriesDaily,
            TotalAiQueriesMonthly: totalAiQueriesMonthly,
            TotalFileStorageBytes: totalStorageBytes,
            TotalFileStorageMegabytes: Math.Round(totalStorageMb, 2)
        );
    }
}
