using FluentAssertions;
using Moq;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Caching;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Shared.Constants;

namespace ProjectBrain.Api.Tests;

public class CoachSpecialismOptionServiceTests
{
    private readonly Mock<ICoachSpecialismOptionRepository> _mockRepository = new();
    private readonly Mock<ICacheService> _mockCache = new();
    private readonly CoachSpecialismOptionService _service;

    public CoachSpecialismOptionServiceTests()
    {
        _service = new CoachSpecialismOptionService(
            _mockRepository.Object,
            _mockCache.Object);
    }

    [Fact]
    public async Task GetActiveNamesAsync_OnCacheMiss_ShouldLoadFromRepositoryAndCache()
    {
        var catalog = CoachSpecialismCatalog.DefaultOptions.ToList();

        _mockCache
            .Setup(c => c.GetAsync<List<string>>("coachspecialismoptions:active", It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<string>?)null);

        _mockRepository
            .Setup(r => r.GetActiveNamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalog);

        var result = await _service.GetActiveNamesAsync();

        result.Should().BeEquivalentTo(catalog);
        _mockRepository.Verify(r => r.GetActiveNamesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(
            c => c.SetAsync(
                "coachspecialismoptions:active",
                catalog,
                TimeSpan.FromHours(24),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetActiveNamesAsync_OnCacheHit_ShouldNotCallRepository()
    {
        var cached = new List<string> { "ADHD", "Autism" };

        _mockCache
            .Setup(c => c.GetAsync<List<string>>("coachspecialismoptions:active", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await _service.GetActiveNamesAsync();

        result.Should().BeEquivalentTo(cached);
        _mockRepository.Verify(r => r.GetActiveNamesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockCache.Verify(
            c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<List<string>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetActiveNamesAsync_WhenRepositoryReturnsEmpty_ShouldNotCache()
    {
        _mockCache
            .Setup(c => c.GetAsync<List<string>>("coachspecialismoptions:active", It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<string>?)null);

        _mockRepository
            .Setup(r => r.GetActiveNamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _service.GetActiveNamesAsync();

        result.Should().BeEmpty();
        _mockRepository.Verify(r => r.GetActiveNamesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(
            c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<List<string>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateSpecialismsAsync_OnCacheHit_ShouldNotCallRepository()
    {
        var cached = CoachSpecialismCatalog.DefaultOptions.ToList();

        _mockCache
            .Setup(c => c.GetAsync<List<string>>("coachspecialismoptions:active", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        await _service.ValidateSpecialismsAsync(["ADHD", "Autism"]);

        _mockRepository.Verify(r => r.GetActiveNamesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidateSpecialismsAsync_WithInvalidSpecialism_ShouldThrow()
    {
        var catalog = CoachSpecialismCatalog.DefaultOptions.ToList();

        _mockCache
            .Setup(c => c.GetAsync<List<string>>("coachspecialismoptions:active", It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalog);

        var act = () => _service.ValidateSpecialismsAsync(["NotARealSpecialism"]);

        await act.Should().ThrowAsync<ProjectBrain.Domain.Exceptions.AppException>()
            .Where(e => e.ErrorCode == "INVALID_SPECIALISM");
    }
}
