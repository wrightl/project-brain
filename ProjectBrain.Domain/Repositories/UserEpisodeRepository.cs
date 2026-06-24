namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

public class UserEpisodeRepository : Repository<UserEpisode, Guid>, IUserEpisodeRepository
{
    public UserEpisodeRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<UserEpisode?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
    }

    public async Task<UserEpisode?> GetByContentHashAsync(string userId, string contentHash, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(
                x => x.UserId == userId
                     && x.ContentHash == contentHash
                     && x.Status != MemoryStatuses.Superseded
                     && x.Status != MemoryStatuses.Rejected,
                cancellationToken);
    }

    public async Task<List<UserEpisode>> GetForUserByStatusesAsync(
        string userId,
        IReadOnlyList<string> statuses,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Where(x => x.UserId == userId && statuses.Contains(x.Status))
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserEpisode>> SearchActiveByContentAsync(
        string userId,
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var pattern = $"%{query}%";
        return await _dbSet.AsNoTracking()
            .Where(x => x.UserId == userId
                        && x.Status == MemoryStatuses.Active
                        && (EF.Functions.Like(x.Summary, pattern) || EF.Functions.Like(x.Topic, pattern)))
            .OrderByDescending(x => x.UpdatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserEpisode>> GetDecayCandidatesAsync(string? userId, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(x =>
            x.Status == MemoryStatuses.Provisional || x.Status == MemoryStatuses.Active);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(x => x.UserId == userId);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task TouchRetrievedAsync(IReadOnlyList<Guid> episodeIds, CancellationToken cancellationToken = default)
    {
        if (episodeIds.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        await _dbSet
            .Where(x => episodeIds.Contains(x.Id))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.LastRetrievedAt, now),
                cancellationToken);
    }
}
