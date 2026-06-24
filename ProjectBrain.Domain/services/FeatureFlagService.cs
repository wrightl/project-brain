using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Caching;

public static class FeatureFlags
{
    public const string EnableCoachSection = "CoachFeatureEnabled";
    public const string EmailsEnabled = "EmailFeatureEnabled";
    public const string AgentFeatureEnabled = "AgentFeatureEnabled";

    public const string DbKeyPrefix = "FeatureFlag:";
    public const string Category = "FeatureFlag";

    public static IReadOnlyList<FeatureFlagDefinition> Definitions { get; } =
    [
        new(EnableCoachSection, "Coach section", "Show coach-related features in the app"),
        new(EmailsEnabled, "Email delivery", "Enable outbound email via Mailgun"),
        new(AgentFeatureEnabled, "AI agent", "Enable the conversational AI agent in chat"),
    ];

    public static IReadOnlyList<string> GetAllKeys() =>
        Definitions.Select(definition => definition.Key).ToList();

    public static bool IsKnownKey(string flagKey) =>
        Definitions.Any(definition => definition.Key == flagKey);
}

public record FeatureFlagDefinition(string Key, string Label, string Description);

public class FeatureFlagResolvedCacheEntry
{
    public bool Enabled { get; set; }
}

public class FeatureFlagMapCacheEntry
{
    public Dictionary<string, bool> Flags { get; set; } = new();
}

public interface IFeatureFlagService
{
    Task<bool> IsCoachSectionEnabled();
    Task<bool> IsEmailingEnabled();
    Task<bool> IsFeatureEnabled(string featureFlag);
    Task<IReadOnlyDictionary<string, bool>> GetAllFlagsAsync(CancellationToken cancellationToken = default);
    Task InvalidateFeatureFlagCacheAsync(CancellationToken cancellationToken = default);
}

public class AzureAppConfigFeatureFlagService : IFeatureFlagService
{
    private const string ResolvedCacheKeyPrefix = "featureflags:resolved:";
    private const string AllFlagsCacheKey = "featureflags:all";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    private readonly IFeatureManager _featureManager;
    private readonly IApplicationSettingsService _applicationSettingsService;
    private readonly ICacheService _cache;
    private readonly ILogger<AzureAppConfigFeatureFlagService> _logger;

    public AzureAppConfigFeatureFlagService(
        IFeatureManager featureManager,
        IApplicationSettingsService applicationSettingsService,
        ICacheService cache,
        ILogger<AzureAppConfigFeatureFlagService> logger)
    {
        _featureManager = featureManager;
        _applicationSettingsService = applicationSettingsService;
        _cache = cache;
        _logger = logger;
    }

    public Task<bool> IsCoachSectionEnabled() =>
        IsFeatureEnabled(FeatureFlags.EnableCoachSection);

    public Task<bool> IsEmailingEnabled() =>
        IsFeatureEnabled(FeatureFlags.EmailsEnabled);

    public async Task<bool> IsFeatureEnabled(string featureFlag)
    {
        var cacheKey = $"{ResolvedCacheKeyPrefix}{featureFlag}";
        var cached = await _cache.GetAsync<FeatureFlagResolvedCacheEntry>(cacheKey);
        if (cached != null)
        {
            return cached.Enabled;
        }

        var enabled = await ResolveFeatureEnabledAsync(featureFlag);
        await _cache.SetAsync(cacheKey, new FeatureFlagResolvedCacheEntry { Enabled = enabled }, CacheExpiration);
        return enabled;
    }

    public async Task<IReadOnlyDictionary<string, bool>> GetAllFlagsAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync<FeatureFlagMapCacheEntry>(AllFlagsCacheKey, cancellationToken);
        if (cached != null)
        {
            return cached.Flags;
        }

        var flags = new Dictionary<string, bool>();
        foreach (var flagKey in FeatureFlags.GetAllKeys())
        {
            flags[flagKey] = await IsFeatureEnabled(flagKey);
        }

        await _cache.SetAsync(AllFlagsCacheKey, new FeatureFlagMapCacheEntry { Flags = flags }, CacheExpiration, cancellationToken);
        return flags;
    }

    public async Task InvalidateFeatureFlagCacheAsync(CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(AllFlagsCacheKey, cancellationToken);

        foreach (var flagKey in FeatureFlags.GetAllKeys())
        {
            await _cache.RemoveAsync($"{ResolvedCacheKeyPrefix}{flagKey}", cancellationToken);
        }

        _logger.LogInformation("Feature flag cache invalidated");
    }

    private async Task<bool> ResolveFeatureEnabledAsync(string featureFlag)
    {
        var dbKey = $"{FeatureFlags.DbKeyPrefix}{featureFlag}";
        var dbValue = await _applicationSettingsService.GetSettingAsync(dbKey);
        if (dbValue != null && bool.TryParse(dbValue, out var enabled))
        {
            return enabled;
        }

        return await _featureManager.IsEnabledAsync(featureFlag);
    }
}
