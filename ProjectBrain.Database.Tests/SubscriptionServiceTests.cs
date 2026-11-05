using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Caching;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

namespace ProjectBrain.Database.Tests;

public class SubscriptionServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly SubscriptionService _service;
    private readonly Mock<IStripeService> _stripe;
    private readonly Mock<ICacheService> _cache;
    private readonly SubscriptionTier _proTier;

    public SubscriptionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options, new Mock<ILogger<AppDbContext>>().Object);

        _proTier = new SubscriptionTier
        {
            Id = 1,
            Name = "Pro",
            UserType = UserType.User.ToString()
        };
        _context.SubscriptionTiers.Add(_proTier);
        _context.SaveChanges();

        _stripe = new Mock<IStripeService>();
        _stripe
            .Setup(s => s.ScheduleCancellationAtPeriodEndAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _stripe
            .Setup(s => s.CancelSubscriptionAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _cache = new Mock<ICacheService>();
        _cache
            .Setup(c => c.GetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _cache
            .Setup(c => c.GetAsync<SubscriptionSettingsInfo>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionSettingsInfo?)null);
        _cache
            .Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _cache
            .Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<SubscriptionSettingsInfo>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _cache
            .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new SubscriptionService(
            new UserSubscriptionRepository(_context),
            _context,
            _stripe.Object,
            new Mock<ILogger<SubscriptionService>>().Object,
            new UnitOfWork(_context),
            _cache.Object);
    }

    [Fact]
    public async Task CancelSubscriptionAtPeriodEndAsync_KeepsPaidAccessUntilPeriodEnd()
    {
        var userId = "auth0|paid";
        var stripeSubId = "sub_paid";
        var periodEnd = DateTime.UtcNow.AddDays(20);
        await AddActiveStripeSubscription(userId, stripeSubId, periodEnd);

        await _service.CancelSubscriptionAtPeriodEndAsync(userId, UserType.User);

        _stripe.Verify(s => s.ScheduleCancellationAtPeriodEndAsync(stripeSubId), Times.Once);
        _stripe.Verify(s => s.CancelSubscriptionAsync(It.IsAny<string>()), Times.Never);

        var stored = await _context.UserSubscriptions.SingleAsync();
        stored.Status.Should().Be("active");
        stored.CanceledAt.Should().NotBeNull();
        stored.CurrentPeriodEnd.Should().BeCloseTo(periodEnd, TimeSpan.FromSeconds(1));

        var tier = await _service.GetUserTierAsync(userId, UserType.User);
        tier.Should().Be("Pro");
    }

    [Fact]
    public async Task GetUserTierAsync_AfterPeriodEndCancel_ReturnsFreeOncePeriodEnds()
    {
        var userId = "auth0|expired-cancel";
        await AddActiveStripeSubscription(userId, "sub_ended", DateTime.UtcNow.AddMinutes(-1));

        await _service.CancelSubscriptionAtPeriodEndAsync(userId, UserType.User);

        var tier = await _service.GetUserTierAsync(userId, UserType.User);
        tier.Should().Be("Free");
    }

    [Fact]
    public async Task CancelSubscriptionAsync_CancelsStripeImmediatelyAndRevokesAccess()
    {
        var userId = "auth0|erase";
        var stripeSubId = "sub_erase";
        await AddActiveStripeSubscription(userId, stripeSubId, DateTime.UtcNow.AddDays(20));

        await _service.CancelSubscriptionAsync(userId, UserType.User);

        _stripe.Verify(s => s.CancelSubscriptionAsync(stripeSubId), Times.Once);
        _stripe.Verify(s => s.ScheduleCancellationAtPeriodEndAsync(It.IsAny<string>()), Times.Never);

        var stored = await _context.UserSubscriptions.SingleAsync();
        stored.Status.Should().Be("canceled");
        stored.CanceledAt.Should().NotBeNull();

        var tier = await _service.GetUserTierAsync(userId, UserType.User);
        tier.Should().Be("Free");
    }

    private async Task AddActiveStripeSubscription(string userId, string stripeSubscriptionId, DateTime periodEnd)
    {
        _context.UserSubscriptions.Add(new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserType = UserType.User.ToString(),
            TierId = _proTier.Id,
            StripeSubscriptionId = stripeSubscriptionId,
            Status = "active",
            CurrentPeriodStart = DateTime.UtcNow.AddDays(-10),
            CurrentPeriodEnd = periodEnd,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
