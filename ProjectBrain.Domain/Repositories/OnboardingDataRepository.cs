namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository implementation for OnboardingData entity
/// </summary>
public class OnboardingDataRepository : Repository<OnboardingData, int>, IOnboardingDataRepository
{
    public OnboardingDataRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<OnboardingData?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(od => od.UserId == userId, cancellationToken);
    }
}

