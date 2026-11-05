using FluentAssertions;
using ProjectBrain.Domain;

namespace ProjectBrain.Api.Tests;

public class SubscriptionTrialExpiryTests
{
    [Fact]
    public void IsPaidAccessStatus_ExpiredTrial_ReturnsFalse()
    {
        var now = new DateTime(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            UserType = "User",
            TierId = 1,
            Status = "trialing",
            TrialEndsAt = now.AddDays(-1),
            CurrentPeriodStart = now.AddDays(-8),
            CurrentPeriodEnd = now.AddDays(-1),
            CreatedAt = now.AddDays(-8),
            UpdatedAt = now.AddDays(-8)
        };

        SubscriptionService.IsPaidAccessStatus(subscription, now).Should().BeFalse();
    }

    [Fact]
    public void IsPaidAccessStatus_ActiveTrial_ReturnsTrue()
    {
        var now = new DateTime(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            UserType = "User",
            TierId = 1,
            Status = "trialing",
            TrialEndsAt = now.AddDays(3),
            CurrentPeriodStart = now.AddDays(-4),
            CurrentPeriodEnd = now.AddDays(3),
            CreatedAt = now.AddDays(-4),
            UpdatedAt = now.AddDays(-4)
        };

        SubscriptionService.IsPaidAccessStatus(subscription, now).Should().BeTrue();
    }

    [Fact]
    public void IsPaidAccessStatus_ActiveSubscription_ReturnsTrue()
    {
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            UserType = "User",
            TierId = 1,
            Status = "active",
            CurrentPeriodStart = DateTime.UtcNow.AddDays(-10),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(20),
            CreatedAt = DateTime.UtcNow.AddDays(-40),
            UpdatedAt = DateTime.UtcNow
        };

        SubscriptionService.IsPaidAccessStatus(subscription).Should().BeTrue();
    }
}
