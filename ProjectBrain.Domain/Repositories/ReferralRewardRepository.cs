namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

public class ReferralRewardRepository : Repository<ReferralReward, Guid>, IReferralRewardRepository
{
    public ReferralRewardRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<ReferralReward>> ListForInviteAsync(Guid referralInviteId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(rr => rr.ReferralInviteId == referralInviteId)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAppliedForBeneficiaryAsync(string beneficiaryUserId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .CountAsync(rr => rr.BeneficiaryUserId == beneficiaryUserId && rr.Status == "Applied", cancellationToken);
    }

    public async Task<ReferralReward?> GetForInviteAndBeneficiaryAsync(Guid referralInviteId, string beneficiaryUserId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(
                rr => rr.ReferralInviteId == referralInviteId && rr.BeneficiaryUserId == beneficiaryUserId,
                cancellationToken);
    }
}

