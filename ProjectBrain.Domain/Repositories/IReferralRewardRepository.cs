namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

public interface IReferralRewardRepository : IRepository<ReferralReward, Guid>
{
    Task<List<ReferralReward>> ListForInviteAsync(Guid referralInviteId, CancellationToken cancellationToken = default);
    Task<int> CountAppliedForBeneficiaryAsync(string beneficiaryUserId, CancellationToken cancellationToken = default);
    Task<ReferralReward?> GetForInviteAndBeneficiaryAsync(Guid referralInviteId, string beneficiaryUserId, CancellationToken cancellationToken = default);
}

