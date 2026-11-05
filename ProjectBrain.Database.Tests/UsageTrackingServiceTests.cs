using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Domain;
using ProjectBrain.Domain.UnitOfWork;

namespace ProjectBrain.Database.Tests;

public class UsageTrackingServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UsageTrackingService _service;
    private const string UserId = "auth0|storage-user";

    public UsageTrackingServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options, new Mock<ILogger<AppDbContext>>().Object);
        _service = new UsageTrackingService(
            _context,
            new UnitOfWork(_context),
            new Mock<ILogger<UsageTrackingService>>().Object);
    }

    [Fact]
    public async Task TrackFileDeleteAsync_DecrementsStoredBytes_AndDoesNotGoBelowZero()
    {
        await _service.TrackFileUploadAsync(UserId, 100);
        await _service.TrackFileUploadAsync(UserId, 50);

        (await _service.GetFileStorageUsageAsync(UserId)).Should().Be(150);

        await _service.TrackFileDeleteAsync(UserId, 40);
        (await _service.GetFileStorageUsageAsync(UserId)).Should().Be(110);

        await _service.TrackFileDeleteAsync(UserId, 500);
        (await _service.GetFileStorageUsageAsync(UserId)).Should().Be(0);
    }

    [Fact]
    public async Task TrackFileDeleteAsync_WhenNoUsageRow_DoesNotThrow()
    {
        await _service.TrackFileDeleteAsync(UserId, 25);

        (await _service.GetFileStorageUsageAsync(UserId)).Should().Be(0);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
