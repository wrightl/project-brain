namespace ProjectBrain.Domain.Repositories;

/// <summary>
/// Repository interface for UserSubscription entity with domain-specific queries
/// </summary>
public interface IUserSubscriptionRepository : IRepository<UserSubscription, Guid>
{
    /// <summary>
    /// Gets the most recent subscription for a user and user type
    /// </summary>
    Task<UserSubscription?> GetLatestForUserAsync(string userId, UserType userType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a subscription by Stripe subscription ID
    /// </summary>
    Task<UserSubscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user is excluded from subscription requirements
    /// </summary>
    Task<bool> IsUserExcludedAsync(string userId, UserType userType, CancellationToken cancellationToken = default);
}

