namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

public class StatisticsService : IStatisticsService
{
    private readonly AppDbContext _context;
    private readonly IUserActivityService _userActivityService;

    public StatisticsService(AppDbContext context, IUserActivityService userActivityService)
    {
        _context = context;
        _userActivityService = userActivityService;
    }

    public async Task<int> GetUserConversationsCountAsync(string userId, string? period = null)
    {
        var query = _context.Conversations
            .Where(c => c.UserId == userId);

        return await applyPeriodFilter(period, query, c => c.CreatedAt);
    }

    public async Task<int> GetAllConversationsCountAsync(string? period = null)
    {
        var query = _context.Conversations.AsQueryable();

        return await applyPeriodFilter(period, query, c => c.CreatedAt);
    }

    public async Task<int> GetUserResourcesCountAsync(string userId)
    {
        return await _context.Resources
            .Where(r => r.UserId == userId && r.IsShared == false)
            .CountAsync();
    }

    public async Task<int> GetUserVoiceNotesCountAsync(string userId)
    {
        return await _context.VoiceNotes
            .Where(vn => vn.UserId == userId)
            .CountAsync();
    }

    public async Task<int> GetCoachClientsCountAsync(string coachId)
    {
        return await _context.Connections
            .Where(c => c.CoachId == coachId && c.Status == "accepted")
            .CountAsync();
    }

    public async Task<int> GetPendingClientsCountAsync(string coachId)
    {
        return await _context.Connections
            .Where(c => c.CoachId == coachId && c.Status == "pending")
            .CountAsync();
    }

    public async Task<int> GetSharedResourcesCountAsync()
    {
        return await _context.Resources
            .Where(r => r.IsShared && (r.UserId == null || r.UserId == string.Empty))
            .CountAsync();
    }

    public async Task<int> GetAllUsersCountAsync()
    {
        return await _context.Users.CountAsync();
    }

    public async Task<int> GetCoachesCountAsync()
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .Where(u => u.UserRoles.Any(ur => ur.RoleName.ToLower() == "coach"))
            .CountAsync();
    }

    public async Task<int> GetNormalUsersCountAsync()
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .Where(u => !u.UserRoles.Any(ur => ur.RoleName.ToLower() == "coach" || ur.RoleName.ToLower() == "admin"))
            .CountAsync();
    }

    public async Task<int> GetQuizzesCountAsync()
    {
        return await _context.Quizzes.CountAsync();
    }

    public async Task<int> GetQuizResponsesCountAsync(string? period = null)
    {
        var query = _context.QuizResponses.AsQueryable();

        return await applyPeriodFilter(period, query, qr => qr.CreatedAt);
    }

    public async Task<int> GetLoggedInUsersCountAsync()
    {
        // Use UserActivityService which handles Redis + database fallback
        return await _userActivityService.GetActiveUsersCountAsync();
    }

    public async Task<int> GetConversationsCountAsync(string? period = null)
    {
        var query = _context.Conversations.AsQueryable();

        return await applyPeriodFilter(period, query, c => c.CreatedAt);
    }

    private static async Task<int> applyPeriodFilter<T>(string? period, IQueryable<T> query, Expression<Func<T, DateTime>> dateSelector)
    {
        if (!string.IsNullOrEmpty(period))
        {
            var now = DateTime.UtcNow;
            var startDate = GetStartDateForPeriod(period, now);

            if (startDate.HasValue)
            {
                if (period.ToLower() == "lastmonth")
                {
                    var thisMonthStart = new DateTime(now.Year, now.Month, 1);
                    var lastMonthStart = thisMonthStart.AddMonths(-1);
                    var lastMonthEnd = thisMonthStart.AddTicks(-1);

                    // Build expression: dateSelector >= lastMonthStart && dateSelector <= lastMonthEnd
                    var parameter = dateSelector.Parameters[0];
                    var dateProperty = dateSelector.Body;
                    var lastMonthStartConstant = Expression.Constant(lastMonthStart);
                    var lastMonthEndConstant = Expression.Constant(lastMonthEnd);
                    var greaterThanOrEqual = Expression.GreaterThanOrEqual(dateProperty, lastMonthStartConstant);
                    var lessThanOrEqual = Expression.LessThanOrEqual(dateProperty, lastMonthEndConstant);
                    var combined = Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);
                    var lambda = Expression.Lambda<Func<T, bool>>(combined, parameter);

                    return await query.Where(lambda).CountAsync();
                }

                // Build expression: dateSelector >= startDate.Value
                var param = dateSelector.Parameters[0];
                var dateProp = dateSelector.Body;
                var startDateConstant = Expression.Constant(startDate.Value);
                var comparison = Expression.GreaterThanOrEqual(dateProp, startDateConstant);
                var filterLambda = Expression.Lambda<Func<T, bool>>(comparison, param);

                return await query.Where(filterLambda).CountAsync();
            }
        }

        return await query.CountAsync();
    }

    private static DateTime? GetStartDateForPeriod(string period, DateTime now)
    {
        return period.ToLower() switch
        {
            "24h" or "last24hours" => now.AddHours(-24),
            "3d" or "last3days" => now.AddDays(-3),
            "7d" or "last7days" => now.AddDays(-7),
            "30d" or "last30days" => now.AddDays(-30),
            "thismonth" => new DateTime(now.Year, now.Month, 1),
            _ => null
        };
    }
}

public interface IStatisticsService
{
    Task<int> GetUserConversationsCountAsync(string userId, string? period = null);
    Task<int> GetAllConversationsCountAsync(string? period = null);
    Task<int> GetUserResourcesCountAsync(string userId);
    Task<int> GetUserVoiceNotesCountAsync(string userId);
    Task<int> GetCoachClientsCountAsync(string coachId);
    Task<int> GetPendingClientsCountAsync(string userId);
    Task<int> GetSharedResourcesCountAsync();
    Task<int> GetAllUsersCountAsync();
    Task<int> GetCoachesCountAsync();
    Task<int> GetNormalUsersCountAsync();
    Task<int> GetQuizzesCountAsync();
    Task<int> GetQuizResponsesCountAsync(string? period = null);
    Task<int> GetLoggedInUsersCountAsync();
    Task<int> GetConversationsCountAsync(string? period = null);
}

