namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository implementation for Tag entity
/// </summary>
public class TagRepository : Repository<Tag, Guid>, ITagRepository
{
    public TagRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Tag?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);
    }

    public async Task<Tag?> GetByNameForUserAsync(string name, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name == name && t.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<Tag>> GetAllForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Tag>> GetByIdsForUserAsync(IEnumerable<Guid> tagIds, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => tagIds.Contains(t.Id) && t.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}

