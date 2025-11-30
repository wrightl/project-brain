namespace ProjectBrain.Domain;

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

