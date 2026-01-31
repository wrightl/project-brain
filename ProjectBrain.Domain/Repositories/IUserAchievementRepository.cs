namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

public interface IUserAchievementRepository : IRepository<UserAchievement, Guid>
{
    Task<UserAchievement?> GetByUserAndAchievementAsync(string userId, Guid achievementId, CancellationToken cancellationToken = default);
    Task<List<UserAchievement>> GetEarnedForUserAsync(string userId, CancellationToken cancellationToken = default);
}

