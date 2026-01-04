namespace ProjectBrain.Domain;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class FeatureGateService : IFeatureGateService
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IUsageTrackingService _usageTrackingService;
    private readonly IConnectionService _connectionService;
    private readonly IResourceService _resourceService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FeatureGateService> _logger;

    public FeatureGateService(
        ISubscriptionService subscriptionService,
        IUsageTrackingService usageTrackingService,
        IConnectionService connectionService,
        IResourceService resourceService,
        IConfiguration configuration,
        ILogger<FeatureGateService> logger)
    {
        _subscriptionService = subscriptionService;
        _usageTrackingService = usageTrackingService;
        _connectionService = connectionService;
        _resourceService = resourceService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> CanUseFeatureAsync(string userId, UserType userType, string feature)
    {
        var (allowed, _) = await CheckFeatureAccessAsync(userId, userType, feature);
        return allowed;
    }

    public async Task<(bool Allowed, string? ErrorMessage)> CheckFeatureAccessAsync(string userId, UserType userType, string feature)
    {
        var tier = await _subscriptionService.GetUserTierAsync(userId, userType);
        var tierLimits = GetTierLimits(userType, tier);

        return feature switch
        {
            "speech_input" => CheckSpeechInputAccess(tierLimits),
            "coach_connections" => await CheckCoachConnectionsAsync(userId, userType, tierLimits),
            "coach_messages" => await CheckCoachMessagesAsync(userId, userType, tierLimits),
            "file_upload" => await CheckFileUploadAsync(userId, userType, tierLimits),
            "external_integrations" => CheckExternalIntegrationsAccess(tierLimits),
            "research_reports" => await CheckResearchReportsAsync(userId, userType, tierLimits),
            "client_connections" => await CheckClientConnectionsAsync(userId, userType, tierLimits),
            "client_messages" => await CheckClientMessagesAsync(userId, userType, tierLimits),
            _ => (true, null) // Unknown features are allowed by default
        };
    }

    private (bool Allowed, string? ErrorMessage) CheckSpeechInputAccess(dynamic tierLimits)
    {
        if (tierLimits.AllowSpeechInput == true)
        {
            return (true, null);
        }
        return (false, "Speech input is only available in Pro and Ultimate tiers. Please upgrade to use this feature.");
    }

    private async Task<(bool Allowed, string? ErrorMessage)> CheckCoachConnectionsAsync(string userId, UserType userType, dynamic tierLimits)
    {
        var maxConnections = (int)tierLimits.MaxCoachConnections;
        if (maxConnections < 0) // Unlimited
        {
            return (true, null);
        }

        // Count current accepted connections
        var connectedCoaches = await _connectionService.GetConnectedCoachIdsAsync(userId);
        var currentConnections = connectedCoaches.Count(c => c.Status == "accepted");

        if (currentConnections >= maxConnections)
        {
            return (false, $"You have reached the maximum of {maxConnections} coach connections. Please upgrade to connect with more coaches.");
        }
        return (true, null);
    }

    private async Task<(bool Allowed, string? ErrorMessage)> CheckCoachMessagesAsync(string userId, UserType userType, dynamic tierLimits)
    {
        var monthlyLimit = (int)tierLimits.MonthlyCoachMessages;
        if (monthlyLimit < 0) // Unlimited
        {
            return (true, null);
        }

        var currentUsage = await _usageTrackingService.GetUsageCountAsync(userId, "coach_message", "monthly");
        if (currentUsage >= monthlyLimit)
        {
            return (false, $"You have reached your monthly limit of {monthlyLimit} messages to coaches. Please upgrade for unlimited messages.");
        }
        return (true, null);
    }

    private async Task<(bool Allowed, string? ErrorMessage)> CheckFileUploadAsync(string userId, UserType userType, dynamic tierLimits)
    {
        var maxFiles = (int)tierLimits.MaxFiles;
        var maxStorageMB = (int)tierLimits.MaxFileStorageMB;

        if (maxFiles >= 0) // Check file count limit
        {
            var userResources = await _resourceService.GetAllForUser(userId);
            var fileCount = userResources.Count(r => !r.IsShared);
            if (fileCount >= maxFiles)
            {
                return (false, $"You have reached the maximum of {maxFiles} files. Please upgrade to upload more files.");
            }
        }

        if (maxStorageMB >= 0) // Check storage limit
        {
            var currentStorage = await _usageTrackingService.GetFileStorageUsageAsync(userId);
            var maxStorageBytes = maxStorageMB * 1024L * 1024L;
            if (currentStorage >= maxStorageBytes)
            {
                return (false, $"You have reached your storage limit of {maxStorageMB}MB. Please upgrade for more storage.");
            }
        }

        return (true, null);
    }

    private (bool Allowed, string? ErrorMessage) CheckExternalIntegrationsAccess(dynamic tierLimits)
    {
        if (tierLimits.AllowExternalIntegrations == true)
        {
            return (true, null);
        }
        return (false, "External integrations are only available in the Ultimate tier. Please upgrade to use this feature.");
    }

    private async Task<(bool Allowed, string? ErrorMessage)> CheckResearchReportsAsync(string userId, UserType userType, dynamic tierLimits)
    {
        var monthlyLimit = (int)tierLimits.MonthlyResearchReports;
        if (monthlyLimit < 0) // Unlimited
        {
            return (true, null);
        }

        var currentUsage = await _usageTrackingService.GetUsageCountAsync(userId, "research_report", "monthly");
        if (currentUsage >= monthlyLimit)
        {
            return (false, $"You have reached your monthly limit of {monthlyLimit} research reports. Please upgrade for more reports.");
        }
        return (true, null);
    }

    private async Task<(bool Allowed, string? ErrorMessage)> CheckClientConnectionsAsync(string userId, UserType userType, dynamic tierLimits)
    {
        var maxConnections = (int)tierLimits.MaxClientConnections;
        if (maxConnections < 0) // Unlimited
        {
            return (true, null);
        }

        var currentConnections = await _usageTrackingService.GetClientConnectionCountAsync(userId);
        if (currentConnections >= maxConnections)
        {
            return (false, $"You have reached the maximum of {maxConnections} client connections. Please upgrade to connect with more clients.");
        }
        return (true, null);
    }

    private async Task<(bool Allowed, string? ErrorMessage)> CheckClientMessagesAsync(string userId, UserType userType, dynamic tierLimits)
    {
        var monthlyLimit = (int)tierLimits.MonthlyClientMessages;
        if (monthlyLimit < 0) // Unlimited
        {
            return (true, null);
        }

        var currentUsage = await _usageTrackingService.GetUsageCountAsync(userId, "client_message", "monthly");
        if (currentUsage >= monthlyLimit)
        {
            return (false, $"You have reached your monthly limit of {monthlyLimit} client messages. Please upgrade for unlimited messages.");
        }
        return (true, null);
    }

    private dynamic GetTierLimits(UserType userType, string tier)
    {
        var configPath = $"TierLimits:{userType.ToString()}:{tier}";

        // Return a dynamic object with tier limits from configuration
        // This is a simplified approach - in production, you might want a more structured approach
        return new
        {
            DailyAIQueries = _configuration[$"{configPath}:DailyAIQueries"] != null ? int.Parse(_configuration[$"{configPath}:DailyAIQueries"]!) : -1,
            MonthlyAIQueries = _configuration[$"{configPath}:MonthlyAIQueries"] != null ? int.Parse(_configuration[$"{configPath}:MonthlyAIQueries"]!) : -1,
            MaxCoachConnections = _configuration[$"{configPath}:MaxCoachConnections"] != null ? int.Parse(_configuration[$"{configPath}:MaxCoachConnections"]!) : -1,
            MonthlyCoachMessages = _configuration[$"{configPath}:MonthlyCoachMessages"] != null ? int.Parse(_configuration[$"{configPath}:MonthlyCoachMessages"]!) : -1,
            MaxFiles = _configuration[$"{configPath}:MaxFiles"] != null ? int.Parse(_configuration[$"{configPath}:MaxFiles"]!) : -1,
            MaxFileStorageMB = _configuration[$"{configPath}:MaxFileStorageMB"] != null ? int.Parse(_configuration[$"{configPath}:MaxFileStorageMB"]!) : -1,
            AllowSpeechInput = _configuration[$"{configPath}:AllowSpeechInput"] == "true",
            AllowExternalIntegrations = _configuration[$"{configPath}:AllowExternalIntegrations"] == "true",
            MonthlyResearchReports = _configuration[$"{configPath}:MonthlyResearchReports"] != null ? int.Parse(_configuration[$"{configPath}:MonthlyResearchReports"]!) : -1,
            MaxClientConnections = _configuration[$"{configPath}:MaxClientConnections"] != null ? int.Parse(_configuration[$"{configPath}:MaxClientConnections"]!) : -1,
            MonthlyClientMessages = _configuration[$"{configPath}:MonthlyClientMessages"] != null ? int.Parse(_configuration[$"{configPath}:MonthlyClientMessages"]!) : -1
        };
    }
}

public interface IFeatureGateService
{
    Task<bool> CanUseFeatureAsync(string userId, UserType userType, string feature);
    Task<(bool Allowed, string? ErrorMessage)> CheckFeatureAccessAsync(string userId, UserType userType, string feature);
}