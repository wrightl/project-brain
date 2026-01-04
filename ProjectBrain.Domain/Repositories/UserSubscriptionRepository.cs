namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository implementation for UserSubscription entity
/// </summary>
public class UserSubscriptionRepository : Repository<UserSubscription, Guid>, IUserSubscriptionRepository
{
    public UserSubscriptionRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<UserSubscription?> GetLatestForUserAsync(string userId, UserType userType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(us => us.Tier)
            .Where(us => us.UserId == userId && us.UserType == userType.ToString())
            .OrderByDescending(us => us.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserSubscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(us => us.Tier)
            .FirstOrDefaultAsync(us => us.StripeSubscriptionId == stripeSubscriptionId, cancellationToken);
    }

    public async Task<bool> IsUserExcludedAsync(string userId, UserType userType, CancellationToken cancellationToken = default)
    {
        return await _context.SubscriptionExclusions
            .AsNoTracking()
            .AnyAsync(se => se.UserId == userId && se.UserType == userType.ToString(), cancellationToken);
    }
}

