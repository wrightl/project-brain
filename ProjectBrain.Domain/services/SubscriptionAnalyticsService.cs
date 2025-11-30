namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class SubscriptionAnalyticsService : ISubscriptionAnalyticsService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubscriptionAnalyticsService> _logger;

    public SubscriptionAnalyticsService(
        AppDbContext context,
        IConfiguration configuration,
        ILogger<SubscriptionAnalyticsService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<int> GetPaidSubscribersCountAsync(string userType, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.UserSubscriptions
            .Where(us => us.UserType == userType &&
                   (us.Status == "active" || us.Status == "trialing"));

        if (startDate.HasValue)
        {
            query = query.Where(us => us.CreatedAt >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            query = query.Where(us => us.CreatedAt <= endDate.Value);
        }

        return await query.CountAsync();
    }

    public async Task<int> GetCancelledSubscriptionsCountAsync(string userType, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.UserSubscriptions
            .Where(us => us.UserType == userType && us.Status == "canceled");

        if (startDate.HasValue)
        {
            query = query.Where(us => us.CanceledAt >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            query = query.Where(us => us.CanceledAt <= endDate.Value);
        }

        return await query.CountAsync();
    }

    public async Task<int> GetExpiredSubscriptionsCountAsync(string userType, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.UserSubscriptions
            .Where(us => us.UserType == userType && us.Status == "expired");

        if (startDate.HasValue)
        {
            query = query.Where(us => us.ExpiredAt >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            query = query.Where(us => us.ExpiredAt <= endDate.Value);
        }

        return await query.CountAsync();
    }

    public async Task<decimal> GetRevenueAsync(string userType, DateTime startDate, DateTime endDate)
    {
        // This is a simplified calculation
        // In production, you'd want to calculate based on actual Stripe invoice data
        // For now, we'll estimate based on subscription periods and prices

        var subscriptions = await _context.UserSubscriptions
            .Include(us => us.Tier)
            .Where(us => us.UserType == userType &&
                   (us.Status == "active" || us.Status == "trialing") &&
                   us.CurrentPeriodStart <= endDate &&
                   us.CurrentPeriodEnd >= startDate)
            .ToListAsync();

        decimal totalRevenue = 0;

        foreach (var subscription in subscriptions)
        {
            // Get price from configuration based on tier and billing period
            // This is simplified - in production, you'd get this from Stripe
            var tierName = subscription.Tier?.Name ?? "Free";
            var isAnnual = subscription.StripePriceId?.Contains("annual", StringComparison.OrdinalIgnoreCase) ?? false;

            decimal monthlyPrice = tierName switch
            {
                "Pro" when userType == "user" => isAnnual ? 10m : 12m,
                "Ultimate" when userType == "user" => isAnnual ? 20m : 24m,
                "Pro" when userType == "coach" => isAnnual ? 50m : 60m,
                _ => 0m
            };

            // Calculate revenue for the period
            var periodStart = subscription.CurrentPeriodStart > startDate ? subscription.CurrentPeriodStart : startDate;
            var periodEnd = subscription.CurrentPeriodEnd < endDate ? subscription.CurrentPeriodEnd : endDate;

            if (periodStart < periodEnd)
            {
                var daysInPeriod = (periodEnd - periodStart).TotalDays;
                var daysInSubscriptionPeriod = (subscription.CurrentPeriodEnd - subscription.CurrentPeriodStart).TotalDays;

                if (daysInSubscriptionPeriod > 0)
                {
                    var proratedRevenue = monthlyPrice * (decimal)(daysInPeriod / daysInSubscriptionPeriod);
                    totalRevenue += proratedRevenue;
                }
            }
        }

        return totalRevenue;
    }

    public async Task<List<RevenueDataPoint>> GetRevenueHistoryAsync(string userType, int months)
    {
        var dataPoints = new List<RevenueDataPoint>();
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddMonths(-months);

        // Group by month
        for (var date = startDate; date <= endDate; date = date.AddMonths(1))
        {
            var monthStart = new DateTime(date.Year, date.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var revenue = await GetRevenueAsync(userType, monthStart, monthEnd);
            dataPoints.Add(new RevenueDataPoint
            {
                Date = monthStart,
                Amount = revenue
            });
        }

        return dataPoints;
    }

    public async Task<List<RevenueDataPoint>> GetPredictedRevenueAsync(string userType, int months)
    {
        // Get historical data for trend analysis
        var historicalMonths = Math.Max(3, months);
        var history = await GetRevenueHistoryAsync(userType, historicalMonths);

        if (history.Count < 2)
        {
            // Not enough data for prediction
            return new List<RevenueDataPoint>();
        }

        // Simple linear regression for prediction
        var predictions = new List<RevenueDataPoint>();
        var lastDate = history.Last().Date;
        var lastAmount = history.Last().Amount;

        // Calculate average growth rate
        var amounts = history.Select(h => h.Amount).ToList();
        var growthRates = new List<decimal>();
        for (int i = 1; i < amounts.Count; i++)
        {
            if (amounts[i - 1] > 0)
            {
                var growthRate = (amounts[i] - amounts[i - 1]) / amounts[i - 1];
                growthRates.Add(growthRate);
            }
        }

        var avgGrowthRate = growthRates.Any() ? growthRates.Average() : 0m;

        // Predict future months
        var currentAmount = lastAmount;
        for (int i = 1; i <= months; i++)
        {
            var predictedDate = lastDate.AddMonths(i);
            currentAmount = currentAmount * (1 + avgGrowthRate);

            predictions.Add(new RevenueDataPoint
            {
                Date = predictedDate,
                Amount = Math.Max(0, currentAmount) // Ensure non-negative
            });
        }

        return predictions;
    }

    public async Task<Dictionary<string, int>> GetSubscriptionsByTierAsync(string userType)
    {
        var subscriptions = await _context.UserSubscriptions
            .Include(us => us.Tier)
            .Where(us => us.UserType == userType &&
                   (us.Status == "active" || us.Status == "trialing"))
            .GroupBy(us => us.Tier!.Name)
            .Select(g => new { Tier = g.Key, Count = g.Count() })
            .ToListAsync();

        return subscriptions.ToDictionary(s => s.Tier, s => s.Count);
    }
}

