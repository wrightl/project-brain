using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

namespace ProjectBrain.Database.Tests;

public class UserFactServicePinTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserFactService _service;
    private const string UserId = "auth0|pin-test";

    public UserFactServicePinTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options, new Mock<ILogger<AppDbContext>>().Object);
        _service = new UserFactService(new UserFactRepository(_context), new UnitOfWork(_context));
    }

    [Fact]
    public async Task PinAsync_SetsPinnedAtAndClearsExpiresAt()
    {
        var fact = new UserFact
        {
            UserId = UserId,
            Content = "Important preference",
            ContentHash = "hash-pin",
            Status = MemoryStatuses.Active,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        _context.UserFacts.Add(fact);
        await _context.SaveChangesAsync();

        var pinned = await _service.PinAsync(UserId, fact.Id);

        pinned.Should().BeTrue();
        var updated = await _context.UserFacts.FindAsync(fact.Id);
        updated!.PinnedAt.Should().NotBeNull();
        updated.ExpiresAt.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
