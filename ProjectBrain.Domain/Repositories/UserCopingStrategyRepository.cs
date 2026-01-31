namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

public class UserCopingStrategyRepository : Repository<UserCopingStrategy, Guid>, IUserCopingStrategyRepository
{
    public UserCopingStrategyRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<UserCopingStrategy>> GetLibraryForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.SavedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserCopingStrategy?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
    }
}

