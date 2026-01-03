namespace ProjectBrain.Domain.Repositories;

/// <summary>
/// Repository interface for OnboardingData entity
/// </summary>
public interface IOnboardingDataRepository : IRepository<OnboardingData, int>
{
    /// <summary>
    /// Gets onboarding data by user ID
    /// </summary>
    Task<OnboardingData?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}

