namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

public class AchievementRepository : Repository<Achievement, Guid>, IAchievementRepository
{
    public AchievementRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<Achievement>> GetAllOrderedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .OrderBy(a => a.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<Achievement?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Key == key, cancellationToken);
    }
}

