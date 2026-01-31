using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

namespace ProjectBrain.Database.Tests;

public class CopingStrategyServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ICopingStrategyService _copingStrategyService;
    private const string TestUserId = "auth0|test-user";
    private const string OtherUserId = "auth0|other-user";

    public CopingStrategyServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockLogger = new Mock<ILogger<AppDbContext>>();
        _context = new AppDbContext(options, mockLogger.Object);

        var repository = new UserCopingStrategyRepository(_context);
        var unitOfWork = new UnitOfWork(_context);
        _copingStrategyService = new CopingStrategyService(repository, unitOfWork);
    }

    [Fact]
    public async Task CreateAsync_ShouldAddStrategyToLibrary()
    {
        // Act
        var created = await _copingStrategyService.CreateAsync(
            TestUserId,
            "Try a short reset",
            "Take 5 minutes to reduce sensory input and breathe slowly.",
            "sparkles");

        // Assert
        created.Should().NotBeNull();
        created.Id.Should().NotBe(Guid.Empty);
        created.UserId.Should().Be(TestUserId);
        created.Title.Should().Be("Try a short reset");
        created.Rating.Should().BeNull();

        var saved = await _context.UserCopingStrategies.FindAsync(created.Id);
        saved.Should().NotBeNull();
        saved!.UserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task UpdateRatingAsync_ShouldUpdateRating_WhenStrategyExistsForUser()
    {
        // Arrange
        var created = await _copingStrategyService.CreateAsync(
            TestUserId,
            "Box breathing",
            "Inhale 4, hold 4, exhale 4, hold 4.",
            "lightbulb");

        // Act
        var updated = await _copingStrategyService.UpdateRatingAsync(
            TestUserId,
            created.Id,
            5);

        // Assert
        updated.Should().NotBeNull();
        updated!.Rating.Should().Be(5);

        var saved = await _context.UserCopingStrategies.FindAsync(created.Id);
        saved!.Rating.Should().Be(5);
    }

    [Fact]
    public async Task UpdateRatingAsync_ShouldReturnNull_WhenStrategyBelongsToDifferentUser()
    {
        // Arrange
        var created = await _copingStrategyService.CreateAsync(
            TestUserId,
            "Grounding",
            "5-4-3-2-1 grounding exercise.",
            null);

        // Act
        var updated = await _copingStrategyService.UpdateRatingAsync(
            OtherUserId,
            created.Id,
            4);

        // Assert
        updated.Should().BeNull();

        var saved = await _context.UserCopingStrategies.FindAsync(created.Id);
        saved!.Rating.Should().BeNull();
    }

    [Fact]
    public async Task UpdateRatingAsync_ShouldThrow_WhenRatingOutOfRange()
    {
        // Act
        var act = () => _copingStrategyService.UpdateRatingAsync(
            TestUserId,
            Guid.NewGuid(),
            6);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

