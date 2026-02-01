namespace ProjectBrain.Domain.Repositories;

using ProjectBrain.Domain.Dtos;

public interface IReferralInviteRepository : IRepository<ReferralInvite, Guid>
{
    Task<ReferralInvite?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<ReferralInvite?> GetByInviterAndRecipientAsync(string inviterUserId, string recipientEmailNormalized, CancellationToken cancellationToken = default);
    Task<List<ReferralInvite>> ListForInviterAsync(string inviterUserId, CancellationToken cancellationToken = default);
}

