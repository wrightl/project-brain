namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository implementation for DeviceToken entity
/// </summary>
public class DeviceTokenRepository : Repository<DeviceToken, Guid>, IDeviceTokenRepository
{
    public DeviceTokenRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<DeviceToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(dt => dt.Token == token, cancellationToken);
    }

    public async Task<IEnumerable<DeviceToken>> GetActiveTokensByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(dt => dt.UserId == userId && dt.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DeviceToken>> GetTokensToValidateAsync(int batchSize, DateTime? lastValidatedBefore = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(dt => dt.IsActive);

        if (lastValidatedBefore.HasValue)
        {
            query = query.Where(dt => dt.LastValidatedAt == null || dt.LastValidatedAt < lastValidatedBefore.Value);
        }

        return await query
            .OrderBy(dt => dt.LastValidatedAt)
            .ThenBy(dt => dt.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DeviceToken>> GetStaleInactiveTokensAsync(DateTime inactiveBefore, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(dt => !dt.IsActive && dt.LastUsedAt < inactiveBefore)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> MarkTokensAsInvalidAsync(IEnumerable<string> tokens, string reason, CancellationToken cancellationToken = default)
    {
        var tokenList = tokens.ToList();
        if (!tokenList.Any())
        {
            return 0;
        }

        // Need to track entities for update (don't use AsNoTracking)
        var tokenEntities = await _dbSet
            .Where(dt => tokenList.Contains(dt.Token))
            .ToListAsync(cancellationToken);

        foreach (var tokenEntity in tokenEntities)
        {
            tokenEntity.IsActive = false;
            tokenEntity.InvalidReason = reason;
            _context.Entry(tokenEntity).Property(dt => dt.IsActive).IsModified = true;
            _context.Entry(tokenEntity).Property(dt => dt.InvalidReason).IsModified = true;
        }

        return tokenEntities.Count;
    }

    public async Task<IEnumerable<DeviceToken>> GetTokensByTokenStringsWithTrackingAsync(IEnumerable<string> tokens, CancellationToken cancellationToken = default)
    {
        var tokenList = tokens.ToList();
        if (!tokenList.Any())
        {
            return Enumerable.Empty<DeviceToken>();
        }

        // Load with tracking enabled for updates
        return await _dbSet
            .Where(dt => tokenList.Contains(dt.Token))
            .ToListAsync(cancellationToken);
    }
}

