namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

public class ReferralInviteRepository : Repository<ReferralInvite, Guid>, IReferralInviteRepository
{
    public ReferralInviteRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<ReferralInvite?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(ri => ri.Inviter)
            .FirstOrDefaultAsync(ri => ri.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<ReferralInvite?> GetByInviterAndRecipientAsync(
        string inviterUserId,
        string recipientEmailNormalized,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(
                ri => ri.InviterUserId == inviterUserId && ri.RecipientEmailNormalized == recipientEmailNormalized,
                cancellationToken);
    }

    public async Task<List<ReferralInvite>> ListForInviterAsync(string inviterUserId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(ri => ri.InviterUserId == inviterUserId)
            .OrderByDescending(ri => ri.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}

