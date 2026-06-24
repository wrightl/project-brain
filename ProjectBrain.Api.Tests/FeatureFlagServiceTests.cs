using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Moq;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Caching;

namespace ProjectBrain.Api.Tests;

public class FeatureFlagServiceTests
{
    private readonly Mock<IFeatureManager> _mockFeatureManager = new();
    private readonly Mock<IApplicationSettingsService> _mockApplicationSettings = new();
    private readonly Mock<ICacheService> _mockCache = new();
    private readonly Mock<ILogger<AzureAppConfigFeatureFlagService>> _mockLogger = new();

    private AzureAppConfigFeatureFlagService CreateService() =>
        new(
            _mockFeatureManager.Object,
            _mockApplicationSettings.Object,
            _mockCache.Object,
            _mockLogger.Object);

    [Fact]
    public async Task IsFeatureEnabled_ReturnsDbOverride_WhenSettingExists()
    {
        _mockCache.Setup(c => c.GetAsync<FeatureFlagResolvedCacheEntry>(It.IsAny<string>(), default))
            .ReturnsAsync((FeatureFlagResolvedCacheEntry?)null);
        _mockApplicationSettings
            .Setup(s => s.GetSettingAsync("FeatureFlag:AgentFeatureEnabled"))
            .ReturnsAsync("true");
        _mockFeatureManager
            .Setup(m => m.IsEnabledAsync("AgentFeatureEnabled"))
            .ReturnsAsync(false);

        var service = CreateService();
        var result = await service.IsFeatureEnabled(FeatureFlags.AgentFeatureEnabled);

        result.Should().BeTrue();
        _mockFeatureManager.Verify(m => m.IsEnabledAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task IsFeatureEnabled_FallsBackToFeatureManager_WhenNoDbOverride()
    {
        _mockCache.Setup(c => c.GetAsync<FeatureFlagResolvedCacheEntry>(It.IsAny<string>(), default))
            .ReturnsAsync((FeatureFlagResolvedCacheEntry?)null);
        _mockApplicationSettings
            .Setup(s => s.GetSettingAsync("FeatureFlag:AgentFeatureEnabled"))
            .ReturnsAsync((string?)null);
        _mockFeatureManager
            .Setup(m => m.IsEnabledAsync("AgentFeatureEnabled"))
            .ReturnsAsync(true);

        var service = CreateService();
        var result = await service.IsFeatureEnabled(FeatureFlags.AgentFeatureEnabled);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsFeatureEnabled_ReturnsCachedValue_OnCacheHit()
    {
        _mockCache.Setup(c => c.GetAsync<FeatureFlagResolvedCacheEntry>("featureflags:resolved:AgentFeatureEnabled", default))
            .ReturnsAsync(new FeatureFlagResolvedCacheEntry { Enabled = false });

        var service = CreateService();
        var result = await service.IsFeatureEnabled(FeatureFlags.AgentFeatureEnabled);

        result.Should().BeFalse();
        _mockApplicationSettings.Verify(s => s.GetSettingAsync(It.IsAny<string>()), Times.Never);
        _mockFeatureManager.Verify(m => m.IsEnabledAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetAllFlagsAsync_ReturnsCachedMap_OnCacheHit()
    {
        var cachedFlags = new Dictionary<string, bool>
        {
            [FeatureFlags.AgentFeatureEnabled] = true
        };

        _mockCache.Setup(c => c.GetAsync<FeatureFlagMapCacheEntry>("featureflags:all", default))
            .ReturnsAsync(new FeatureFlagMapCacheEntry { Flags = cachedFlags });

        var service = CreateService();
        var result = await service.GetAllFlagsAsync();

        result.Should().BeEquivalentTo(cachedFlags);
        _mockApplicationSettings.Verify(s => s.GetSettingAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InvalidateFeatureFlagCacheAsync_RemovesAggregatedAndPerFlagKeys()
    {
        var service = CreateService();
        await service.InvalidateFeatureFlagCacheAsync();

        _mockCache.Verify(c => c.RemoveAsync("featureflags:all", default), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync("featureflags:resolved:CoachFeatureEnabled", default), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync("featureflags:resolved:EmailFeatureEnabled", default), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync("featureflags:resolved:AgentFeatureEnabled", default), Times.Once);
    }
}

public class FeatureFlagSettingsServiceTests
{
    private readonly Mock<IApplicationSettingsService> _mockApplicationSettings = new();
    private readonly Mock<IFeatureFlagService> _mockFeatureFlagService = new();

    private FeatureFlagSettingsService CreateService() =>
        new(_mockApplicationSettings.Object, _mockFeatureFlagService.Object);

    [Fact]
    public async Task GetFeatureFlagSettingsAsync_ReturnsAllFlagsWithMetadata()
    {
        _mockFeatureFlagService
            .Setup(s => s.IsFeatureEnabled(FeatureFlags.EnableCoachSection))
            .ReturnsAsync(true);
        _mockFeatureFlagService
            .Setup(s => s.IsFeatureEnabled(FeatureFlags.EmailsEnabled))
            .ReturnsAsync(false);
        _mockFeatureFlagService
            .Setup(s => s.IsFeatureEnabled(FeatureFlags.AgentFeatureEnabled))
            .ReturnsAsync(true);

        var service = CreateService();
        var result = await service.GetFeatureFlagSettingsAsync();

        result.Should().HaveCount(3);
        result.Should().Contain(item => item.Key == FeatureFlags.EnableCoachSection && item.Enabled);
        result.Should().Contain(item => item.Key == FeatureFlags.EmailsEnabled && !item.Enabled);
        result.Should().Contain(item => item.Key == FeatureFlags.AgentFeatureEnabled && item.Enabled);
        result.Should().OnlyContain(item => !string.IsNullOrWhiteSpace(item.Label));
        result.Should().OnlyContain(item => !string.IsNullOrWhiteSpace(item.Description));
    }

    [Fact]
    public async Task UpdateFeatureFlagSettingsAsync_UpsertsAllFlagsAndInvalidatesCache()
    {
        var flags = new Dictionary<string, bool>
        {
            [FeatureFlags.EnableCoachSection] = true,
            [FeatureFlags.EmailsEnabled] = false,
            [FeatureFlags.AgentFeatureEnabled] = true,
        };

        var service = CreateService();
        await service.UpdateFeatureFlagSettingsAsync(flags, "admin|123");

        _mockApplicationSettings.Verify(
            s => s.UpsertSettingAsync(
                "FeatureFlag:CoachFeatureEnabled",
                "true",
                FeatureFlags.Category,
                It.IsAny<string>(),
                "admin|123"),
            Times.Once);
        _mockApplicationSettings.Verify(
            s => s.UpsertSettingAsync(
                "FeatureFlag:EmailFeatureEnabled",
                "false",
                FeatureFlags.Category,
                It.IsAny<string>(),
                "admin|123"),
            Times.Once);
        _mockApplicationSettings.Verify(
            s => s.UpsertSettingAsync(
                "FeatureFlag:AgentFeatureEnabled",
                "true",
                FeatureFlags.Category,
                It.IsAny<string>(),
                "admin|123"),
            Times.Once);
        _mockFeatureFlagService.Verify(s => s.InvalidateFeatureFlagCacheAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateFeatureFlagSettingsAsync_ThrowsForUnknownKeys()
    {
        var service = CreateService();
        var flags = new Dictionary<string, bool>
        {
            ["UnknownFlag"] = true
        };

        var act = () => service.UpdateFeatureFlagSettingsAsync(flags, "admin|123");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Unknown feature flag keys*");
    }
}
