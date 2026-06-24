using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Dtos;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

namespace ProjectBrain.Database.Tests;

public class ChatMemoryContextServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ChatMemoryContextService _service;
    private readonly Guid _conversationId = Guid.NewGuid();
    private const string UserId = "auth0|memory-test";

    public ChatMemoryContextServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockLogger = new Mock<ILogger<AppDbContext>>();
        _context = new AppDbContext(options, mockLogger.Object);
        var unitOfWork = new UnitOfWork(_context);

        var userProfileService = new UserProfileService(
            new UserProfileRepository(_context),
            _context,
            unitOfWork,
            new Mock<ProjectBrain.Domain.Caching.ICacheService>().Object);

        var cache = new Mock<ProjectBrain.Domain.Caching.ICacheService>();
        cache.Setup(c => c.GetAsync<ApplicationSetting>(It.IsAny<string>())).ReturnsAsync((ApplicationSetting?)null);

        var applicationSettingsService = new ApplicationSettingsService(
            _context,
            unitOfWork,
            cache.Object,
            new Mock<ILogger<ApplicationSettingsService>>().Object);

        _context.ApplicationSettings.Add(new ApplicationSetting
        {
            Key = "AI:RecentMessageWindow",
            Value = "3",
            Category = "AI",
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = "test"
        });

        _context.Users.Add(new User
        {
            Id = UserId,
            Email = "memory@test.com",
            FullName = "Memory Test"
        });

        var profile = new UserProfile { UserId = UserId, PreferredPronoun = "they/them" };
        _context.UserProfiles.Add(profile);
        _context.SaveChanges();

        profile.Preference = new UserPreference
        {
            UserProfileId = profile.Id,
            Preferences = """{"timezone":"UTC"}"""
        };

        _context.Conversations.Add(new Conversation
        {
            Id = _conversationId,
            UserId = UserId,
            Title = "Test",
            ContextSummary = "Prior discussion about routines.",
            SummaryMessageCount = 4,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        _service = new ChatMemoryContextService(
            userProfileService,
            applicationSettingsService,
            new ConversationService(new ConversationRepository(_context), unitOfWork),
            new SqlUserMemoryRetrievalService(
                new UserFactRepository(_context),
                new UserEpisodeRepository(_context)));
    }

    [Fact]
    public async Task BuildAsync_LoadsPreferencesSummaryAndSettings()
    {
        var context = await _service.BuildAsync(UserId, _conversationId);

        context.UserPreferences.Should().NotBeNull();
        context.UserPreferences!.PreferredPronoun.Should().Be("they/them");
        context.UserPreferences.ParsedPreferences.Should().ContainKey("timezone");
        context.ConversationSummary.Should().Be("Prior discussion about routines.");
        context.RecentMessageWindow.Should().Be(3);
    }

    [Fact]
    public async Task BuildAsync_WithoutConversationId_OmitsSummary()
    {
        var context = await _service.BuildAsync(UserId, conversationId: null);

        context.ConversationSummary.Should().BeNull();
        context.UserPreferences.Should().NotBeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
