namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Repositories;

public class AchievementService : IAchievementService
{
    private readonly IUserAchievementRepository _userAchievementRepository;

    public AchievementService(IUserAchievementRepository userAchievementRepository)
    {
        _userAchievementRepository = userAchievementRepository ?? throw new ArgumentNullException(nameof(userAchievementRepository));
    }

    public async Task<List<UserAchievement>> GetEarnedForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _userAchievementRepository.GetEarnedForUserAsync(userId, cancellationToken);
    }
}

