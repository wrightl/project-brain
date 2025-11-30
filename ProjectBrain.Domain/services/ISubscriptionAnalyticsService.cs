namespace ProjectBrain.Domain;

public interface ISubscriptionAnalyticsService
{
    Task<int> GetPaidSubscribersCountAsync(string userType, DateTime? startDate = null, DateTime? endDate = null);
    Task<int> GetCancelledSubscriptionsCountAsync(string userType, DateTime? startDate = null, DateTime? endDate = null);
    Task<int> GetExpiredSubscriptionsCountAsync(string userType, DateTime? startDate = null, DateTime? endDate = null);
    Task<decimal> GetRevenueAsync(string userType, DateTime startDate, DateTime endDate);
    Task<List<RevenueDataPoint>> GetRevenueHistoryAsync(string userType, int months);
    Task<List<RevenueDataPoint>> GetPredictedRevenueAsync(string userType, int months);
    Task<Dictionary<string, int>> GetSubscriptionsByTierAsync(string userType);
}

public class RevenueDataPoint
{
    public required DateTime Date { get; init; }
    public required decimal Amount { get; init; }
}

