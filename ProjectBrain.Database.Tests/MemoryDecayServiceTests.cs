using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Dtos;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

namespace ProjectBrain.Database.Tests;

public class MemoryDecayServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly MemoryDecayService _service;
    private readonly Mock<IApplicationSettingsService> _settings = new();
    private readonly Mock<IUserMemoryIndexService> _indexService = new();
    private const string UserId = "auth0|decay-test";

    public MemoryDecayServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options, new Mock<ILogger<AppDbContext>>().Object);
        var unitOfWork = new UnitOfWork(_context);

        _settings.Setup(s => s.GetMemorySettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemorySettings
            {
                EnableMemoryDecay = true,
                ProvisionalTtlDays = 30,
                ActiveFactTtlDays = 365,
                ActiveEpisodeTtlDays = 180,
                DecayInactivityDays = 90
            });

        _service = new MemoryDecayService(
            _settings.Object,
            new UserFactRepository(_context),
            new UserEpisodeRepository(_context),
            _indexService.Object,
            new MemoryPromotionAuditRepository(_context),
            unitOfWork,
            new Mock<ILogger<MemoryDecayService>>().Object);
    }

    [Fact]
    public async Task ApplyDecayAsync_WhenDisabled_ReturnsZero()
    {
        _settings.Setup(s => s.GetMemorySettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemorySettings { EnableMemoryDecay = false });

        var count = await _service.ApplyDecayAsync(UserId);

        count.Should().Be(0);
    }

    [Fact]
    public async Task ApplyDecayAsync_SupersedesExpiredProvisionalFact()
    {
        var fact = new UserFact
        {
            UserId = UserId,
            Content = "Old provisional fact",
            ContentHash = "hash-1",
            Status = MemoryStatuses.Provisional,
            CreatedAt = DateTime.UtcNow.AddDays(-40),
            UpdatedAt = DateTime.UtcNow.AddDays(-40)
        };
        _context.UserFacts.Add(fact);
        await _context.SaveChangesAsync();

        var count = await _service.ApplyDecayAsync(UserId);

        count.Should().Be(1);
        var updated = await _context.UserFacts.FindAsync(fact.Id);
        updated!.Status.Should().Be(MemoryStatuses.Superseded);
        _indexService.Verify(i => i.DeleteFactAsync(fact.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyDecayAsync_SkipsPinnedFact()
    {
        var fact = new UserFact
        {
            UserId = UserId,
            Content = "Pinned fact",
            ContentHash = "hash-pinned",
            Status = MemoryStatuses.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-400),
            UpdatedAt = DateTime.UtcNow.AddDays(-400),
            PinnedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.UserFacts.Add(fact);
        await _context.SaveChangesAsync();

        var count = await _service.ApplyDecayAsync(UserId);

        count.Should().Be(0);
        var updated = await _context.UserFacts.FindAsync(fact.Id);
        updated!.Status.Should().Be(MemoryStatuses.Active);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
