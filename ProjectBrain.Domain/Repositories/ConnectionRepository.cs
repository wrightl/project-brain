namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository implementation for Connection entity
/// </summary>
public class ConnectionRepository : Repository<Connection, Guid>, IConnectionRepository
{
    public ConnectionRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Connection?> GetByUserAndCoachAsync(string userId, string coachId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CoachId == coachId, cancellationToken);
    }

    public async Task<IEnumerable<Connection>> GetConnectionsAsync(string userId, bool isCoach, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => ((c.UserId == userId && !isCoach) || (c.CoachId == userId && isCoach)) && (c.Status == "accepted" || c.Status == "pending"))
            .Include(c => c.User)
            .Include(c => c.Coach)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Connection>> GetConnectedCoachesAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.UserId == userId && (c.Status == "accepted" || c.Status == "pending"))
            .Include(c => c.User)
            .Include(c => c.Coach)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Connection>> GetConnectedUsersAsync(string coachId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.CoachId == coachId && (c.Status == "accepted" || c.Status == "pending"))
            .Include(c => c.User)
            .Include(c => c.Coach)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Connection>> GetConnectionsByCoachIdAsync(string coachId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.CoachId == coachId && (c.Status == "accepted" || c.Status == "pending"))
            .Include(c => c.User)
            .Include(c => c.Coach)
            .ToListAsync(cancellationToken);
    }

    public async Task<DateTime?> GetEarliestConnectionDateAsync(string userId, CancellationToken cancellationToken = default)
    {
        var earliestConnection = await _dbSet
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return earliestConnection?.CreatedAt;
    }

    public async Task<int> CountConnectionsAsync(string userId, bool isCoach, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .CountAsync(c => ((c.UserId == userId && !isCoach) || (c.CoachId == userId && isCoach)) && (c.Status == "accepted" || c.Status == "pending"), cancellationToken);
    }

    public async Task<IEnumerable<Connection>> GetPagedConnectionsAsync(string userId, bool isCoach, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => ((c.UserId == userId && !isCoach) || (c.CoachId == userId && isCoach)) && (c.Status == "accepted" || c.Status == "pending"))
            .Include(c => c.User)
            .Include(c => c.Coach)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}

