using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Caching;
using ProjectBrain.Domain.Dtos;
using ProjectBrain.Domain.UnitOfWork;

namespace ProjectBrain.Database.Tests;

public class ApplicationSettingsChatPolicyTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ApplicationSettingsService _service;

    public ApplicationSettingsChatPolicyTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockLogger = new Mock<ILogger<AppDbContext>>();
        _context = new AppDbContext(options, mockLogger.Object);
        var unitOfWork = new UnitOfWork(_context);
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.GetAsync<ApplicationSetting>(It.IsAny<string>())).ReturnsAsync((ApplicationSetting?)null);
        cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ApplicationSetting>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        _service = new ApplicationSettingsService(
            _context,
            unitOfWork,
            cache.Object,
            new Mock<ILogger<ApplicationSettingsService>>().Object);

        _context.ApplicationSettings.AddRange(
            new ApplicationSetting
            {
                Key = "AI:Policy:CrisisGuidance",
                Value = "Crisis rule",
                Category = "AI:Policy",
                Description = "Crisis",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "test"
            },
            new ApplicationSetting
            {
                Key = "AI:Policy:CommunicationStyle",
                Value = "Be clear",
                Category = "AI:Policy",
                Description = "Tone",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "test"
            },
            new ApplicationSetting
            {
                Key = "AI:MaxHistoryMessages",
                Value = "8",
                Category = "AI",
                Description = "History",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "test"
            },
            new ApplicationSetting
            {
                Key = "AI:RecentMessageWindow",
                Value = "4",
                Category = "AI",
                Description = "Recent window",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "test"
            },
            new ApplicationSetting
            {
                Key = "AI:ConversationSummaryInterval",
                Value = "6",
                Category = "AI",
                Description = "Summary interval",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "test"
            },
            new ApplicationSetting
            {
                Key = "AI:MaxConversationSummaryLength",
                Value = "1500",
                Category = "AI",
                Description = "Max summary length",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "test"
            },
            new ApplicationSetting
            {
                Key = "AI:EnableConversationSummary",
                Value = "true",
                Category = "AI",
                Description = "Enable summary",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "test"
            });
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetChatPoliciesAsync_ReturnsPolicySettingsOrderedByKey()
    {
        var policies = await _service.GetChatPoliciesAsync();

        policies.Should().HaveCount(2);
        policies.Select(p => p.Key).Should().BeInAscendingOrder();
        policies.Should().Contain(p => p.Key == "AI:Policy:CrisisGuidance" && p.Value == "Crisis rule");
    }

    [Fact]
    public async Task GetAISettingsAsync_IncludesConversationSummaryDefaultsWhenMissing()
    {
        var settings = await _service.GetAISettingsAsync();

        settings.MaxHistoryMessages.Should().Be(8);
        settings.RecentMessageWindow.Should().Be(4);
        settings.ConversationSummaryInterval.Should().Be(6);
        settings.MaxConversationSummaryLength.Should().Be(1500);
        settings.EnableConversationSummary.Should().BeTrue();
    }

    [Fact]
    public async Task GetChatMemorySettingsAsync_ReturnsSeededValues()
    {
        var settings = await _service.GetChatMemorySettingsAsync();

        settings.RecentMessageWindow.Should().Be(4);
        settings.ConversationSummaryInterval.Should().Be(6);
        settings.MaxConversationSummaryLength.Should().Be(1500);
        settings.EnableConversationSummary.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateChatMemorySettingsAsync_PersistsAllMemoryKeys()
    {
        await _service.UpdateChatMemorySettingsAsync(
            new ChatMemorySettings
            {
                RecentMessageWindow = 3,
                ConversationSummaryInterval = 8,
                MaxConversationSummaryLength = 2000,
                EnableConversationSummary = false
            },
            "admin-test");

        var settings = await _service.GetChatMemorySettingsAsync();
        settings.RecentMessageWindow.Should().Be(3);
        settings.ConversationSummaryInterval.Should().Be(8);
        settings.MaxConversationSummaryLength.Should().Be(2000);
        settings.EnableConversationSummary.Should().BeFalse();

        (await _context.ApplicationSettings.FirstAsync(s => s.Key == "AI:EnableConversationSummary"))
            .Value.Should().Be("false");
    }

    [Fact]
    public async Task UpdateChatPoliciesAsync_UpdatesPolicyValues()
    {
        await _service.UpdateChatPoliciesAsync(
            new List<ChatPolicyItem>
            {
                new() { Key = "AI:Policy:CrisisGuidance", Value = "Updated crisis rule" },
                new() { Key = "AI:Policy:CommunicationStyle", Value = "Updated tone rule" }
            },
            "admin-test");

        var policies = await _service.GetChatPoliciesAsync();
        policies.Should().Contain(p => p.Key == "AI:Policy:CrisisGuidance" && p.Value == "Updated crisis rule");
        policies.Should().Contain(p => p.Key == "AI:Policy:CommunicationStyle" && p.Value == "Updated tone rule");
    }

    [Fact]
    public async Task UpdateChatPoliciesAsync_RejectsInvalidKey()
    {
        var act = () => _service.UpdateChatPoliciesAsync(
            new List<ChatPolicyItem>
            {
                new() { Key = "AI:MaxHistoryMessages", Value = "nope" }
            },
            "admin-test");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetMemorySettingsAsync_ReturnsDefaultsWhenMissing()
    {
        var settings = await _service.GetMemorySettingsAsync();
        settings.EnableMemoryFormation.Should().BeTrue();
        settings.MinPromotionConfidence.Should().Be(0.75);
        settings.MaxFactsRetrieved.Should().Be(5);
    }

    [Fact]
    public async Task UpdateMemorySettingsAsync_PersistsValues()
    {
        _context.ApplicationSettings.AddRange(
            new ApplicationSetting
            {
                Key = "AI:Memory:EnableMemoryFormation",
                Value = "true",
                Category = "AI:Memory",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "seed"
            },
            new ApplicationSetting
            {
                Key = "AI:Memory:MinPromotionConfidence",
                Value = "0.75",
                Category = "AI:Memory",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "seed"
            },
            new ApplicationSetting
            {
                Key = "AI:Memory:ProvisionalConfidence",
                Value = "0.60",
                Category = "AI:Memory",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "seed"
            },
            new ApplicationSetting
            {
                Key = "AI:Memory:ActivationObservationCount",
                Value = "2",
                Category = "AI:Memory",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "seed"
            },
            new ApplicationSetting
            {
                Key = "AI:Memory:MaxFactsPerTurn",
                Value = "3",
                Category = "AI:Memory",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "seed"
            },
            new ApplicationSetting
            {
                Key = "AI:Memory:MaxEpisodesPerTurn",
                Value = "2",
                Category = "AI:Memory",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "seed"
            },
            new ApplicationSetting
            {
                Key = "AI:Memory:MaxFactsRetrieved",
                Value = "5",
                Category = "AI:Memory",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "seed"
            },
            new ApplicationSetting
            {
                Key = "AI:Memory:MaxEpisodesRetrieved",
                Value = "3",
                Category = "AI:Memory",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "seed"
            },
            new ApplicationSetting
            {
                Key = "AI:Memory:IndexProvisionalMemories",
                Value = "false",
                Category = "AI:Memory",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "seed"
            },
            new ApplicationSetting
            {
                Key = "AI:Memory:EnableMemoryDecay",
                Value = "true",
                Category = "AI:Memory",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "seed"
            },
            new ApplicationSetting
            {
                Key = "AI:Memory:ProvisionalTtlDays",
                Value = "30",
                Category = "AI:Memory",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "seed"
            },
            new ApplicationSetting
            {
                Key = "AI:Memory:ActiveFactTtlDays",
                Value = "365",
                Category = "AI:Memory",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "seed"
            },
            new ApplicationSetting
            {
                Key = "AI:Memory:ActiveEpisodeTtlDays",
                Value = "180",
                Category = "AI:Memory",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "seed"
            },
            new ApplicationSetting
            {
                Key = "AI:Memory:DecayInactivityDays",
                Value = "90",
                Category = "AI:Memory",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "seed"
            });
        await _context.SaveChangesAsync();

        await _service.UpdateMemorySettingsAsync(
            new MemorySettings
            {
                EnableMemoryFormation = false,
                MinPromotionConfidence = 0.8,
                ProvisionalConfidence = 0.55,
                ActivationObservationCount = 3,
                MaxFactsPerTurn = 2,
                MaxEpisodesPerTurn = 1,
                MaxFactsRetrieved = 4,
                MaxEpisodesRetrieved = 2,
                IndexProvisionalMemories = true
            },
            "admin-test");

        var updated = await _service.GetMemorySettingsAsync();
        updated.EnableMemoryFormation.Should().BeFalse();
        updated.MinPromotionConfidence.Should().Be(0.8);
        updated.IndexProvisionalMemories.Should().BeTrue();
    }

    [Fact]
    public async Task GetMemorySettingsAsync_IncludesDecayDefaultsWhenMissing()
    {
        var settings = await _service.GetMemorySettingsAsync();

        settings.EnableMemoryDecay.Should().BeTrue();
        settings.ProvisionalTtlDays.Should().Be(30);
        settings.ActiveFactTtlDays.Should().Be(365);
        settings.ActiveEpisodeTtlDays.Should().Be(180);
        settings.DecayInactivityDays.Should().Be(90);
    }

    [Fact]
    public async Task UpdateMemorySettingsAsync_PersistsDecayKeys()
    {
        _context.ApplicationSettings.AddRange(
            new ApplicationSetting { Key = "AI:Memory:EnableMemoryFormation", Value = "true", Category = "AI:Memory", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:Memory:MinPromotionConfidence", Value = "0.75", Category = "AI:Memory", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:Memory:ProvisionalConfidence", Value = "0.60", Category = "AI:Memory", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:Memory:ActivationObservationCount", Value = "2", Category = "AI:Memory", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:Memory:MaxFactsPerTurn", Value = "3", Category = "AI:Memory", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:Memory:MaxEpisodesPerTurn", Value = "2", Category = "AI:Memory", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:Memory:MaxFactsRetrieved", Value = "5", Category = "AI:Memory", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:Memory:MaxEpisodesRetrieved", Value = "3", Category = "AI:Memory", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:Memory:IndexProvisionalMemories", Value = "false", Category = "AI:Memory", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:Memory:EnableMemoryDecay", Value = "true", Category = "AI:Memory", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:Memory:ProvisionalTtlDays", Value = "30", Category = "AI:Memory", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:Memory:ActiveFactTtlDays", Value = "365", Category = "AI:Memory", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:Memory:ActiveEpisodeTtlDays", Value = "180", Category = "AI:Memory", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:Memory:DecayInactivityDays", Value = "90", Category = "AI:Memory", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" });
        await _context.SaveChangesAsync();

        await _service.UpdateMemorySettingsAsync(
            new MemorySettings
            {
                EnableMemoryDecay = false,
                ProvisionalTtlDays = 14,
                ActiveFactTtlDays = 200,
                ActiveEpisodeTtlDays = 120,
                DecayInactivityDays = 60
            },
            "admin-test");

        var updated = await _service.GetMemorySettingsAsync();
        updated.EnableMemoryDecay.Should().BeFalse();
        updated.ProvisionalTtlDays.Should().Be(14);
        updated.DecayInactivityDays.Should().Be(60);
    }

    [Fact]
    public async Task GetPromptBudgetSettingsAsync_ReturnsDefaultsWhenMissing()
    {
        var settings = await _service.GetPromptBudgetSettingsAsync();

        settings.EnablePromptBudget.Should().BeFalse();
        settings.SystemReserve.Should().BeGreaterThan(0);
        settings.HistoryReserve.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdatePromptBudgetSettingsAsync_PersistsValues()
    {
        _context.ApplicationSettings.AddRange(
            new ApplicationSetting { Key = "AI:EnablePromptBudget", Value = "false", Category = "AI", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:PromptBudget:SystemReserve", Value = "400", Category = "AI", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:PromptBudget:PoliciesReserve", Value = "300", Category = "AI", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:PromptBudget:PreferencesReserve", Value = "200", Category = "AI", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:PromptBudget:QueryReserve", Value = "200", Category = "AI", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:PromptBudget:SummaryReserve", Value = "400", Category = "AI", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:PromptBudget:FactsReserve", Value = "300", Category = "AI", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:PromptBudget:EpisodesReserve", Value = "300", Category = "AI", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:PromptBudget:OnboardingReserve", Value = "500", Category = "AI", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" },
            new ApplicationSetting { Key = "AI:PromptBudget:HistoryReserve", Value = "800", Category = "AI", UpdatedAt = DateTime.UtcNow, UpdatedBy = "seed" });
        await _context.SaveChangesAsync();

        await _service.UpdatePromptBudgetSettingsAsync(
            new PromptBudgetSettings
            {
                EnablePromptBudget = true,
                SystemReserve = 500,
                PoliciesReserve = 250,
                PreferencesReserve = 200,
                QueryReserve = 200,
                SummaryReserve = 350,
                FactsReserve = 300,
                EpisodesReserve = 250,
                OnboardingReserve = 450,
                HistoryReserve = 900
            },
            "admin-test");

        var updated = await _service.GetPromptBudgetSettingsAsync();
        updated.EnablePromptBudget.Should().BeTrue();
        updated.SystemReserve.Should().Be(500);
        updated.HistoryReserve.Should().Be(900);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
