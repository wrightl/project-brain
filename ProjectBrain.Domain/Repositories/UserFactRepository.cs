namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

public class UserFactRepository : Repository<UserFact, Guid>, IUserFactRepository
{
    public UserFactRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<UserFact?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
    }

    public async Task<UserFact?> GetByContentHashAsync(string userId, string contentHash, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(
                x => x.UserId == userId
                     && x.ContentHash == contentHash
                     && x.Status != MemoryStatuses.Superseded
                     && x.Status != MemoryStatuses.Rejected,
                cancellationToken);
    }

    public async Task<List<UserFact>> GetActiveForUserAsync(string userId, int limit, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == MemoryStatuses.Active)
            .OrderByDescending(x => x.UpdatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserFact>> GetForUserByStatusesAsync(
        string userId,
        IReadOnlyList<string> statuses,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Where(x => x.UserId == userId && statuses.Contains(x.Status))
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserFact>> SearchActiveByContentAsync(
        string userId,
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var pattern = $"%{query}%";
        return await _dbSet.AsNoTracking()
            .Where(x => x.UserId == userId
                        && x.Status == MemoryStatuses.Active
                        && EF.Functions.Like(x.Content, pattern))
            .OrderByDescending(x => x.UpdatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
