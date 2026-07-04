namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository implementation for UserProfile entity
/// </summary>
public class UserProfileRepository : Repository<UserProfile, int>, IUserProfileRepository
{
    public UserProfileRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(up => up.UserId == userId, cancellationToken);
    }

    public async Task<UserProfile?> GetByUserIdWithRelatedAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(up => up.NeurodiverseTraits)
            .Include(up => up.Preference)
            .Include(cp => cp.User!)
            .Include(up => up.User.UserRoles)
            .FirstOrDefaultAsync(up => up.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<UserProfile>> GetByUserIdsWithRelatedAsync(
        IEnumerable<string> userIds,
        CancellationToken cancellationToken = default)
    {
        var idList = userIds.Distinct().ToList();
        if (idList.Count == 0)
        {
            return Array.Empty<UserProfile>();
        }

        return await _dbSet
            .AsNoTracking()
            .Include(up => up.NeurodiverseTraits)
            .Include(up => up.Preference)
            .Where(up => idList.Contains(up.UserId))
            .ToListAsync(cancellationToken);
    }
}

