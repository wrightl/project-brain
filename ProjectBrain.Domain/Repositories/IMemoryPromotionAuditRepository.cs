namespace ProjectBrain.Domain.Repositories;

public interface IMemoryPromotionAuditRepository : IRepository<MemoryPromotionAudit, Guid>
{
    Task<int> DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
