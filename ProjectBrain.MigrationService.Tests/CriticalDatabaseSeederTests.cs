using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Database;
using ProjectBrain.Shared.Constants;
using ProjectBrain.MigrationService.Seeding;

namespace ProjectBrain.MigrationService.Tests;

public class CriticalDatabaseSeederTests : IDisposable
{
    private readonly AppDbContext _context;

    public CriticalDatabaseSeederTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var mockContextLogger = new Mock<ILogger<AppDbContext>>();
        _context = new AppDbContext(options, mockContextLogger.Object);
    }

    [Fact]
    public async Task SeedRolesAsync_CreatesDefaultRoles_WhenMissing()
    {
        var logger = new Mock<ILogger>();

        await CriticalDatabaseSeeder.SeedRolesAsync(_context, logger.Object);

        (await _context.Roles.CountAsync()).Should().Be(3);
        _context.Roles.Select(r => r.Name).Should().BeEquivalentTo(
            [AppRoles.User, AppRoles.Coach, AppRoles.Admin]);
    }

    [Fact]
    public async Task SeedSubscriptionTiersAsync_CreatesDefaultTiers_WhenMissing()
    {
        var logger = new Mock<ILogger>();

        await CriticalDatabaseSeeder.SeedSubscriptionTiersAsync(_context, logger.Object);

        (await _context.SubscriptionTiers.CountAsync()).Should().Be(5);
    }

    public void Dispose() => _context.Dispose();
}
