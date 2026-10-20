using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Caching;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

namespace ProjectBrain.Database.Tests;

public class UserProfileServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserProfileService _service;
    private const string UserId = "auth0|profile-test";

    public UserProfileServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockLogger = new Mock<ILogger<AppDbContext>>();
        _context = new AppDbContext(options, mockLogger.Object);
        var unitOfWork = new UnitOfWork(_context);
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.GetAsync<UserProfile>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);
        cache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new UserProfileService(
            new UserProfileRepository(_context),
            _context,
            unitOfWork,
            cache.Object);

        _context.Users.Add(new User
        {
            Id = UserId,
            Email = "profile@test.com",
            FullName = "Profile Test"
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateOrUpdate_PreferencesOnly_ShouldPreserveNeurodiverseTraits()
    {
        await _service.CreateOrUpdate(
            UserId,
            doB: new DateOnly(1990, 1, 1),
            preferredPronoun: "they/them",
            neurodiverseTraits: ["ADHD", "Autism"],
            preferences: new Dictionary<string, object> { ["theme"] = "standard" });

        // Theme/timezone endpoints only pass preferences — the previous
        // implementation treated omitted traits as "clear all".
        await _service.CreateOrUpdate(
            UserId,
            preferences: new Dictionary<string, object>
            {
                ["theme"] = "dark",
                ["timezone"] = "Europe/London"
            });

        var traits = await LoadTraitsAsync();
        var preferenceJson = await LoadPreferencesAsync();

        traits.Should().BeEquivalentTo(["ADHD", "Autism"]);
        preferenceJson.Should().Contain("dark");
        preferenceJson.Should().Contain("Europe/London");
    }

    [Fact]
    public async Task CreateOrUpdate_TraitsOnly_ShouldPreservePreferences()
    {
        await _service.CreateOrUpdate(
            UserId,
            neurodiverseTraits: ["ADHD"],
            preferences: new Dictionary<string, object> { ["theme"] = "colourful" });

        await _service.CreateOrUpdate(UserId, neurodiverseTraits: ["ADHD", "Dyslexia"]);

        var traits = await LoadTraitsAsync();
        var preferenceJson = await LoadPreferencesAsync();

        traits.Should().BeEquivalentTo(["ADHD", "Dyslexia"]);
        preferenceJson.Should().Contain("colourful");
    }

    [Fact]
    public async Task CreateOrUpdate_EmptyTraitsList_ShouldClearTraits()
    {
        await _service.CreateOrUpdate(UserId, neurodiverseTraits: ["ADHD"]);

        await _service.CreateOrUpdate(UserId, neurodiverseTraits: []);

        var traits = await LoadTraitsAsync();
        traits.Should().BeEmpty();
    }

    private async Task<List<string>> LoadTraitsAsync()
    {
        var profile = await _context.UserProfiles.AsNoTracking()
            .FirstAsync(p => p.UserId == UserId);
        return await _context.NeurodiverseTraits.AsNoTracking()
            .Where(t => t.UserProfileId == profile.Id)
            .Select(t => t.Trait)
            .ToListAsync();
    }

    private async Task<string?> LoadPreferencesAsync()
    {
        var profile = await _context.UserProfiles.AsNoTracking()
            .FirstAsync(p => p.UserId == UserId);
        return await _context.UserPreferences.AsNoTracking()
            .Where(p => p.UserProfileId == profile.Id)
            .Select(p => p.Preferences)
            .FirstOrDefaultAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
