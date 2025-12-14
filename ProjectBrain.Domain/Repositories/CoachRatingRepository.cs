namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;
using ProjectBrain.Database.Models;

/// <summary>
/// Repository implementation for CoachRating entity
/// </summary>
public class CoachRatingRepository : Repository<CoachRating, Guid>, ICoachRatingRepository
{
    public CoachRatingRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<CoachRating?> GetByUserAndCoachAsync(string userId, string coachId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.CoachId == coachId, cancellationToken);
    }

    public async Task<IEnumerable<CoachRating>> GetRatingsByCoachIdAsync(string coachId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.CoachId == coachId)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CoachRating>> GetPagedRatingsByCoachIdAsync(string coachId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.CoachId == coachId)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountRatingsByCoachIdAsync(string coachId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .CountAsync(r => r.CoachId == coachId, cancellationToken);
    }

    public async Task<double?> GetAverageRatingByCoachIdAsync(string coachId, CancellationToken cancellationToken = default)
    {
        var average = await _dbSet
            .AsNoTracking()
            .Where(r => r.CoachId == coachId)
            .AverageAsync(r => (double?)r.Rating, cancellationToken);

        return average;
    }
}

