using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Dtos;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

namespace ProjectBrain.Database.Tests;

public class MemoryPromotionServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly MemoryPromotionService _service;
    private readonly Mock<IUserMemoryIndexService> _indexService = new();
    private const string UserId = "auth0|memory-promo";

    public MemoryPromotionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options, new Mock<ILogger<AppDbContext>>().Object);
        var unitOfWork = new UnitOfWork(_context);

        _service = new MemoryPromotionService(
            new UserFactRepository(_context),
            new UserEpisodeRepository(_context),
            new MemoryPromotionAuditRepository(_context),
            unitOfWork,
            _indexService.Object,
            new Mock<ILogger<MemoryPromotionService>>().Object);
    }

    [Fact]
    public async Task ProcessExtractionAsync_RejectsBelowProvisionalConfidence()
    {
        var settings = DefaultSettings();
        await _service.ProcessExtractionAsync(
            UserId,
            Guid.NewGuid(),
            new MemoryExtractionResult
            {
                Facts =
                [
                    new ExtractedFactCandidate
                    {
                        Content = "User prefers short answers",
                        Category = "preference",
                        Confidence = 0.4
                    }
                ]
            },
            settings);

        _context.UserFacts.Should().BeEmpty();
        _context.MemoryPromotionAudits.Should().ContainSingle(a => a.Decision == "rejected");
    }

    [Fact]
    public async Task ProcessExtractionAsync_PromotesHighConfidenceFact()
    {
        var settings = DefaultSettings();
        await _service.ProcessExtractionAsync(
            UserId,
            Guid.NewGuid(),
            new MemoryExtractionResult
            {
                Facts =
                [
                    new ExtractedFactCandidate
                    {
                        Content = "User works hybrid on Tuesdays",
                        Category = "work_context",
                        Confidence = 0.9
                    }
                ]
            },
            settings);

        var fact = _context.UserFacts.Single();
        fact.Status.Should().Be(MemoryStatuses.Active);
        fact.Content.Should().Be("User works hybrid on Tuesdays");
        _indexService.Verify(s => s.IndexFactAsync(It.IsAny<UserFact>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessExtractionAsync_DuplicateProvisional_IncrementsObservationAndPromotes()
    {
        var settings = DefaultSettings();
        var hash = MemoryHashHelper.ComputeHash("User prefers bullet lists");
        _context.UserFacts.Add(new UserFact
        {
            UserId = UserId,
            Content = "User prefers bullet lists",
            Category = "preference",
            Status = MemoryStatuses.Provisional,
            Confidence = 0.65,
            ContentHash = hash,
            ObservationCount = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await _service.ProcessExtractionAsync(
            UserId,
            Guid.NewGuid(),
            new MemoryExtractionResult
            {
                Facts =
                [
                    new ExtractedFactCandidate
                    {
                        Content = "User prefers bullet lists",
                        Category = "preference",
                        Confidence = 0.65
                    }
                ]
            },
            settings);

        var fact = _context.UserFacts.Single();
        fact.Status.Should().Be(MemoryStatuses.Active);
        fact.ObservationCount.Should().Be(2);
    }

    [Fact]
    public async Task ProcessExtractionAsync_WhenDisabled_DoesNothing()
    {
        var settings = DefaultSettings();
        settings.EnableMemoryFormation = false;

        await _service.ProcessExtractionAsync(
            UserId,
            Guid.NewGuid(),
            new MemoryExtractionResult
            {
                Facts =
                [
                    new ExtractedFactCandidate
                    {
                        Content = "Should not be stored",
                        Confidence = 0.95
                    }
                ]
            },
            settings);

        _context.UserFacts.Should().BeEmpty();
    }

    private static MemorySettings DefaultSettings() => new()
    {
        EnableMemoryFormation = true,
        MinPromotionConfidence = 0.75,
        ProvisionalConfidence = 0.60,
        ActivationObservationCount = 2,
        MaxFactsPerTurn = 3,
        MaxEpisodesPerTurn = 2,
        MaxFactsRetrieved = 5,
        MaxEpisodesRetrieved = 3
    };

    public void Dispose()
    {
        _context.Dispose();
    }
}
