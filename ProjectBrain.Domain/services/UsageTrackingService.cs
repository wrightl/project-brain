namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectBrain.Domain.UnitOfWork;

public class UsageTrackingService : IUsageTrackingService
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UsageTrackingService> _logger;

    public UsageTrackingService(AppDbContext context, IUnitOfWork unitOfWork, ILogger<UsageTrackingService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task TrackAIQueryAsync(string userId)
    {
        await TrackUsageAsync(userId, "ai_query", "daily");
        await TrackUsageAsync(userId, "ai_query", "monthly");
    }

    public async Task TrackCoachMessageAsync(string userId)
    {
        await TrackUsageAsync(userId, "coach_message", "monthly");
    }

    public async Task TrackClientMessageAsync(string coachId)
    {
        await TrackUsageAsync(coachId, "client_message", "monthly");
    }

    public async Task TrackFileUploadAsync(string userId, long fileSize)
    {
        await TrackUsageAsync(userId, "file_upload", "monthly");

        // Update file storage usage (get tracked entity for potential update)
        var storageUsage = await _context.FileStorageUsages
            .FirstOrDefaultAsync(fsu => fsu.UserId == userId);

        if (storageUsage == null)
        {
            storageUsage = new FileStorageUsage
            {
                UserId = userId,
                TotalBytes = fileSize,
                UpdatedAt = DateTime.UtcNow
            };
            _context.FileStorageUsages.Add(storageUsage);
        }
        else
        {
            storageUsage.TotalBytes += fileSize;
            storageUsage.UpdatedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task TrackResearchReportAsync(string userId)
    {
        await TrackUsageAsync(userId, "research_report", "monthly");
    }

    private async Task TrackUsageAsync(string userId, string usageType, string periodType)
    {
        var periodStart = GetPeriodStart(periodType);

        var tracking = await _context.UsageTrackings
            .FirstOrDefaultAsync(ut =>
                ut.UserId == userId &&
                ut.UsageType == usageType &&
                ut.PeriodType == periodType &&
                ut.PeriodStart == periodStart);

        if (tracking == null)
        {
            tracking = new UsageTracking
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UsageType = usageType,
                PeriodType = periodType,
                PeriodStart = periodStart,
                Count = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UsageTrackings.Add(tracking);
        }
        else
        {
            tracking.Count++;
            tracking.UpdatedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private static DateTime GetPeriodStart(string periodType)
    {
        var now = DateTime.UtcNow;
        return periodType switch
        {
            "daily" => new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc),
            "monthly" => new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => throw new ArgumentException($"Unknown period type: {periodType}")
        };
    }

    public async Task<int> GetUsageCountAsync(string userId, string usageType, string periodType)
    {
        var periodStart = GetPeriodStart(periodType);

        var tracking = await _context.UsageTrackings
            .FirstOrDefaultAsync(ut =>
                ut.UserId == userId &&
                ut.UsageType == usageType &&
                ut.PeriodType == periodType &&
                ut.PeriodStart == periodStart);

        return tracking?.Count ?? 0;
    }

    public async Task<long> GetFileStorageUsageAsync(string userId)
    {
        var storageUsage = await _context.FileStorageUsages
            .AsNoTracking()
            .FirstOrDefaultAsync(fsu => fsu.UserId == userId);

        return storageUsage?.TotalBytes ?? 0;
    }

    public async Task<bool> CheckLimitAsync(string userId, string userType, string usageType, int limit, string periodType)
    {
        if (limit < 0) // -1 means unlimited
        {
            return true;
        }

        var currentUsage = await GetUsageCountAsync(userId, usageType, periodType);
        return currentUsage < limit;
    }

    public async Task<int> GetClientConnectionCountAsync(string coachId)
    {
        return await _context.Connections
            .AsNoTracking()
            .Where(c => c.CoachId == coachId && c.Status == "accepted")
            .CountAsync();
    }
}

public interface IUsageTrackingService
{
    Task TrackAIQueryAsync(string userId);
    Task TrackCoachMessageAsync(string userId);
    Task TrackClientMessageAsync(string coachId);
    Task TrackFileUploadAsync(string userId, long fileSize);
    Task TrackResearchReportAsync(string userId);
    Task<int> GetUsageCountAsync(string userId, string usageType, string periodType);
    Task<long> GetFileStorageUsageAsync(string userId);
    Task<bool> CheckLimitAsync(string userId, string userType, string usageType, int limit, string periodType);
    Task<int> GetClientConnectionCountAsync(string coachId);
}