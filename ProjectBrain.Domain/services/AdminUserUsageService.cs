namespace ProjectBrain.Domain;

using Microsoft.Extensions.Configuration;
using ProjectBrain.Domain.Repositories;

public interface IAdminUserUsageService
{
    Task<AdminUserUsageResponse?> GetUserUsageAsync(string userId, CancellationToken cancellationToken = default);
}

public class AdminUserUsageService : IAdminUserUsageService
{
    private readonly IUserRepository _userRepository;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IUsageTrackingService _usageTrackingService;
    private readonly IResourceRepository _resourceRepository;
    private readonly IConnectionService _connectionService;
    private readonly IConfiguration _configuration;

    public AdminUserUsageService(
        IUserRepository userRepository,
        ISubscriptionService subscriptionService,
        IUsageTrackingService usageTrackingService,
        IResourceRepository resourceRepository,
        IConnectionService connectionService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _subscriptionService = subscriptionService;
        _usageTrackingService = usageTrackingService;
        _resourceRepository = resourceRepository;
        _connectionService = connectionService;
        _configuration = configuration;
    }

    public async Task<AdminUserUsageResponse?> GetUserUsageAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(userId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        var isCoach = user.UserRoles.Any(r => r.RoleName.Equals("coach", StringComparison.OrdinalIgnoreCase));
        var userTypeEnum = isCoach ? UserType.Coach : UserType.User;
        var userType = isCoach ? "coach" : "user";

        var tier = await _subscriptionService.GetUserTierAsync(userId, userTypeEnum);

        var dailyAIQueries = await _usageTrackingService.GetUsageCountAsync(userId, "ai_query", "daily");
        var monthlyAIQueries = await _usageTrackingService.GetUsageCountAsync(userId, "ai_query", "monthly");
        var monthlyCoachMessages = await _usageTrackingService.GetUsageCountAsync(userId, "coach_message", "monthly");
        var monthlyClientMessages = await _usageTrackingService.GetUsageCountAsync(userId, "client_message", "monthly");
        var monthlyResearchReports = await _usageTrackingService.GetUsageCountAsync(userId, "research_report", "monthly");
        var monthlyFileUploads = await _usageTrackingService.GetUsageCountAsync(userId, "file_upload", "monthly");
        var fileStorageBytes = await _usageTrackingService.GetFileStorageUsageAsync(userId);

        var fileCount = await _resourceRepository.CountForUserAsync(userId, cancellationToken);

        var connectedCoaches = await _connectionService.GetConnectedCoachIdsAsync(userId);
        var acceptedCoachConnections = connectedCoaches.Count(c => c.Status.Equals("accepted", StringComparison.OrdinalIgnoreCase));

        var connectedUsers = await _connectionService.GetConnectedUserIdsAsync(userId);
        var acceptedClientConnections = connectedUsers.Count(c => c.Status.Equals("accepted", StringComparison.OrdinalIgnoreCase));

        var tierLimitRoot = userTypeEnum == UserType.Coach
            ? $"TierLimits:Coach:{tier}"
            : $"TierLimits:User:{tier}";

        int? GetIntLimit(string key)
        {
            var value = _configuration[$"{tierLimitRoot}:{key}"];
            return int.TryParse(value, out var parsed) ? parsed : null;
        }

        return new AdminUserUsageResponse
        {
            UserId = userId,
            UserType = userType,
            Tier = tier,
            Usage = new AdminUserUsage
            {
                AiQueries = new AdminUserUsageAiQueries
                {
                    Daily = dailyAIQueries,
                    Monthly = monthlyAIQueries
                },
                CoachMessages = new AdminUserUsageCoachMessages
                {
                    Monthly = monthlyCoachMessages
                },
                ClientMessages = new AdminUserUsageClientMessages
                {
                    Monthly = monthlyClientMessages
                },
                ResearchReports = new AdminUserUsageResearchReports
                {
                    Monthly = monthlyResearchReports
                },
                FileUploads = new AdminUserUsageFileUploads
                {
                    Monthly = monthlyFileUploads
                },
                FileStorage = new AdminUserUsageFileStorage
                {
                    Bytes = fileStorageBytes,
                    Megabytes = fileStorageBytes / (1024.0 * 1024.0)
                },
                Files = new AdminUserUsageFiles
                {
                    TotalCount = fileCount
                },
                Connections = new AdminUserUsageConnections
                {
                    CoachAcceptedCount = acceptedCoachConnections,
                    ClientAcceptedCount = acceptedClientConnections
                }
            },
            Limits = new AdminUserUsageLimits
            {
                DailyAIQueries = GetIntLimit("DailyAIQueries"),
                MonthlyAIQueries = GetIntLimit("MonthlyAIQueries"),
                MaxCoachConnections = GetIntLimit("MaxCoachConnections"),
                MonthlyCoachMessages = GetIntLimit("MonthlyCoachMessages"),
                MaxFiles = GetIntLimit("MaxFiles"),
                MaxFileStorageMB = GetIntLimit("MaxFileStorageMB"),
                MonthlyResearchReports = GetIntLimit("MonthlyResearchReports"),
                MaxClientConnections = GetIntLimit("MaxClientConnections"),
                MonthlyClientMessages = GetIntLimit("MonthlyClientMessages"),
                FileUploadsMonthly = null
            }
        };
    }
}

public class AdminUserUsageResponse
{
    public required string UserId { get; init; }
    public required string UserType { get; init; } // "user" | "coach"
    public required string Tier { get; init; }
    public required AdminUserUsage Usage { get; init; }
    public required AdminUserUsageLimits Limits { get; init; }
}

public class AdminUserUsage
{
    public required AdminUserUsageAiQueries AiQueries { get; init; }
    public required AdminUserUsageCoachMessages CoachMessages { get; init; }
    public required AdminUserUsageClientMessages ClientMessages { get; init; }
    public required AdminUserUsageResearchReports ResearchReports { get; init; }
    public required AdminUserUsageFileUploads FileUploads { get; init; }
    public required AdminUserUsageFileStorage FileStorage { get; init; }
    public required AdminUserUsageFiles Files { get; init; }
    public required AdminUserUsageConnections Connections { get; init; }
}

public class AdminUserUsageAiQueries
{
    public required int Daily { get; init; }
    public required int Monthly { get; init; }
}

public class AdminUserUsageCoachMessages
{
    public required int Monthly { get; init; }
}

public class AdminUserUsageClientMessages
{
    public required int Monthly { get; init; }
}

public class AdminUserUsageResearchReports
{
    public required int Monthly { get; init; }
}

public class AdminUserUsageFileUploads
{
    public required int Monthly { get; init; }
}

public class AdminUserUsageFileStorage
{
    public required long Bytes { get; init; }
    public required double Megabytes { get; init; }
}

public class AdminUserUsageFiles
{
    public required int TotalCount { get; init; }
}

public class AdminUserUsageConnections
{
    public required int CoachAcceptedCount { get; init; }
    public required int ClientAcceptedCount { get; init; }
}

public class AdminUserUsageLimits
{
    public int? DailyAIQueries { get; init; }
    public int? MonthlyAIQueries { get; init; }
    public int? MaxCoachConnections { get; init; }
    public int? MonthlyCoachMessages { get; init; }
    public int? MaxFiles { get; init; }
    public int? MaxFileStorageMB { get; init; }
    public int? MonthlyResearchReports { get; init; }
    public int? MaxClientConnections { get; init; }
    public int? MonthlyClientMessages { get; init; }
    public int? FileUploadsMonthly { get; init; }
}

