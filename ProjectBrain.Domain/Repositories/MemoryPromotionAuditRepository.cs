namespace ProjectBrain.Domain.Repositories;

public class MemoryPromotionAuditRepository : Repository<MemoryPromotionAudit, Guid>, IMemoryPromotionAuditRepository
{
    public MemoryPromotionAuditRepository(AppDbContext context) : base(context)
    {
    }
}
