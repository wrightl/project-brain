namespace ProjectBrain.Domain;

public interface IFeatureFlagSettingsService
{
    Task<IReadOnlyList<FeatureFlagSettingItem>> GetFeatureFlagSettingsAsync(CancellationToken cancellationToken = default);
    Task UpdateFeatureFlagSettingsAsync(IReadOnlyDictionary<string, bool> flags, string updatedBy, CancellationToken cancellationToken = default);
}

public class FeatureFlagSettingItem
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public required string Description { get; init; }
    public required bool Enabled { get; init; }
}

public class FeatureFlagSettingsService : IFeatureFlagSettingsService
{
    private readonly IApplicationSettingsService _applicationSettingsService;
    private readonly IFeatureFlagService _featureFlagService;

    public FeatureFlagSettingsService(
        IApplicationSettingsService applicationSettingsService,
        IFeatureFlagService featureFlagService)
    {
        _applicationSettingsService = applicationSettingsService;
        _featureFlagService = featureFlagService;
    }

    public async Task<IReadOnlyList<FeatureFlagSettingItem>> GetFeatureFlagSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        var items = new List<FeatureFlagSettingItem>();

        foreach (var definition in FeatureFlags.Definitions)
        {
            var enabled = await _featureFlagService.IsFeatureEnabled(definition.Key);
            items.Add(new FeatureFlagSettingItem
            {
                Key = definition.Key,
                Label = definition.Label,
                Description = definition.Description,
                Enabled = enabled
            });
        }

        return items;
    }

    public async Task UpdateFeatureFlagSettingsAsync(
        IReadOnlyDictionary<string, bool> flags,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        var unknownKeys = flags.Keys.Where(key => !FeatureFlags.IsKnownKey(key)).ToList();
        if (unknownKeys.Count > 0)
        {
            throw new InvalidOperationException($"Unknown feature flag keys: {string.Join(", ", unknownKeys)}");
        }

        foreach (var definition in FeatureFlags.Definitions)
        {
            if (!flags.TryGetValue(definition.Key, out var enabled))
            {
                throw new InvalidOperationException($"Missing feature flag value for '{definition.Key}'");
            }

            var dbKey = $"{FeatureFlags.DbKeyPrefix}{definition.Key}";
            await _applicationSettingsService.UpsertSettingAsync(
                dbKey,
                enabled.ToString().ToLowerInvariant(),
                FeatureFlags.Category,
                definition.Description,
                updatedBy);
        }

        await _featureFlagService.InvalidateFeatureFlagCacheAsync(cancellationToken);
    }
}
