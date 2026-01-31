namespace ProjectBrain.Domain;

public interface IAchievementService
{
    Task<List<UserAchievement>> GetEarnedForUserAsync(string userId, CancellationToken cancellationToken = default);
}

