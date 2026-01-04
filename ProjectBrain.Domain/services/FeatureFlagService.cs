using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

public static class FeatureFlags
{
    public const string EnableCoachSection = "CoachFeatureEnabled";
    public const string EmailsEnabled = "EmailFeatureEnabled";
    public const string AgentFeatureEnabled = "AgentFeatureEnabled";
}

public interface IFeatureFlagService
{
    // Task<bool> GetBoolFlag(string flagKey, bool defaultValue = false);
    // Task<string> GetStringFlag(string flagKey, string defaultValue = "");
    Task<bool> IsCoachSectionEnabled();
    Task<bool> IsEmailingEnabled();
    Task<bool> IsFeatureEnabled(string featureFlag);
}

public class AzureAppConfigFeatureFlagService : IFeatureFlagService
{
    private readonly IFeatureManager _featureManager;
    private readonly ILogger<AzureAppConfigFeatureFlagService> _logger;

    public AzureAppConfigFeatureFlagService(IFeatureManager featureManager, ILogger<AzureAppConfigFeatureFlagService> logger)
    {
        _featureManager = featureManager;
        _logger = logger;
    }

    public async Task<bool> IsCoachSectionEnabled()
    {
        return await _featureManager.IsEnabledAsync(FeatureFlags.EnableCoachSection);
    }

    public async Task<bool> IsEmailingEnabled()
    {
        return await _featureManager.IsEnabledAsync(FeatureFlags.EmailsEnabled);
    }

    public async Task<bool> IsFeatureEnabled(string featureFlag)
    {
        return await _featureManager.IsEnabledAsync(featureFlag);
    }
}