namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

public class SystemTagRepository : Repository<SystemTag, Guid>, ISystemTagRepository
{
    public SystemTagRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<SystemTag>> GetAllWithFieldsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SystemTags
            .AsNoTracking()
            .Include(st => st.FieldDefinitions)
            .OrderBy(st => st.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SystemTag>> GetByIdsAsync(IEnumerable<Guid> systemTagIds, CancellationToken cancellationToken = default)
    {
        var ids = systemTagIds?.Distinct().ToList() ?? new List<Guid>();
        if (ids.Count == 0)
        {
            return new List<SystemTag>();
        }

        return await _context.SystemTags
            .AsNoTracking()
            .Where(st => ids.Contains(st.Id))
            .ToListAsync(cancellationToken);
    }
}

