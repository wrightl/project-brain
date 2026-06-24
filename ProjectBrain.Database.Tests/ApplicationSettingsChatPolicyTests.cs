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

    public void Dispose()
    {
        _context.Dispose();
    }
}
