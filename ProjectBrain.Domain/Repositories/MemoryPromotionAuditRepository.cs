namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

public class MemoryPromotionAuditRepository : Repository<MemoryPromotionAudit, Guid>, IMemoryPromotionAuditRepository
{
    public MemoryPromotionAuditRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<int> DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
    }
}
