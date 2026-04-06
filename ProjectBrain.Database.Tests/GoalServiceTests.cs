using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Database.Models;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

namespace ProjectBrain.Database.Tests;

public class GoalServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IGoalService _goalService;

    public GoalServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockLogger = new Mock<ILogger<AppDbContext>>();
        _context = new AppDbContext(options, mockLogger.Object);
        var repository = new GoalRepository(_context);
        var unitOfWork = new UnitOfWork(_context);
        _goalService = new GoalService(repository, unitOfWork);
    }

    [Fact]
    public async Task GetPrioritizedIncompleteGoalBacklogAsync_GroupsByNormalizedText_AndSortsByRecencyThenMissCount()
    {
        var userId = "auth0|goals";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var d1 = today.AddDays(-5);
        var d2 = today.AddDays(-3);
        var d3 = today.AddDays(-2);

        _context.Goals.AddRange(
            NewGoal(userId, d1, 0, "  Drink water  ", false),
            NewGoal(userId, d2, 0, "DRINK WATER", false),
            NewGoal(userId, d3, 1, "Walk 10 minutes", false),
            NewGoal(userId, d3, 2, "Walk 10 minutes", false));

        await _context.SaveChangesAsync();

        var backlog = await _goalService.GetPrioritizedIncompleteGoalBacklogAsync(userId, maxItems: 10);

        backlog.Should().HaveCount(2);
        backlog[0].Message.Should().Be("Walk 10 minutes");
        backlog[0].MissCount.Should().Be(2);
        backlog[0].LastMissedDate.Should().Be(d3);

        backlog[1].Message.Should().Be("DRINK WATER");
        backlog[1].MissCount.Should().Be(2);
        backlog[1].LastMissedDate.Should().Be(d2);
    }

    [Fact]
    public async Task GetPrioritizedIncompleteGoalBacklogAsync_WhenLastMissedSameDate_SortsByHigherMissCountFirst()
    {
        var userId = "auth0|tie";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var d = today.AddDays(-1);

        _context.Goals.AddRange(
            NewGoal(userId, d, 0, "A", false),
            NewGoal(userId, d, 1, "A", false),
            NewGoal(userId, d, 2, "B", false));

        await _context.SaveChangesAsync();

        var backlog = await _goalService.GetPrioritizedIncompleteGoalBacklogAsync(userId, maxItems: 10);

        backlog[0].Message.Should().Be("A");
        backlog[0].MissCount.Should().Be(2);
        backlog[1].Message.Should().Be("B");
        backlog[1].MissCount.Should().Be(1);
    }

    [Fact]
    public async Task GetPrioritizedIncompleteGoalBacklogAsync_ExcludesTodayAndCompleted()
    {
        var userId = "auth0|filter";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        _context.Goals.AddRange(
            NewGoal(userId, today, 0, "Today open", false),
            NewGoal(userId, today.AddDays(-1), 0, "Done yesterday", true),
            NewGoal(userId, today.AddDays(-1), 1, "Missed yesterday", false));

        await _context.SaveChangesAsync();

        var backlog = await _goalService.GetPrioritizedIncompleteGoalBacklogAsync(userId, maxItems: 10);

        backlog.Should().ContainSingle(b => b.Message == "Missed yesterday");
        backlog.Should().NotContain(b => b.Message == "Today open");
        backlog.Should().NotContain(b => b.Message == "Done yesterday");
    }

    [Fact]
    public async Task GetPrioritizedIncompleteGoalBacklogAsync_RespectsMaxItems()
    {
        var userId = "auth0|cap";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        for (var i = 0; i < 20; i++)
        {
            _context.Goals.Add(NewGoal(userId, today.AddDays(-1 - i), 0, $"goal-{i}", false));
        }

        await _context.SaveChangesAsync();

        var backlog = await _goalService.GetPrioritizedIncompleteGoalBacklogAsync(userId, maxItems: 15);

        backlog.Should().HaveCount(15);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static Goal NewGoal(string userId, DateOnly date, int index, string message, bool completed) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = date,
            Index = index,
            Message = message,
            Completed = completed,
            CompletedAt = completed ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
}
