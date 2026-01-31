namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

public class UserAchievementRepository : Repository<UserAchievement, Guid>, IUserAchievementRepository
{
    public UserAchievementRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<UserAchievement?> GetByUserAndAchievementAsync(string userId, Guid achievementId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.AchievementId == achievementId,
                cancellationToken);
    }

    public async Task<List<UserAchievement>> GetEarnedForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(x => x.Achievement!)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.EarnedAt)
            .ToListAsync(cancellationToken);
    }
}

