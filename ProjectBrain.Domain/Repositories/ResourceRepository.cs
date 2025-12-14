namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository implementation for Resource entity
/// </summary>
public class ResourceRepository : Repository<Resource, Guid>, IResourceRepository
{
    public ResourceRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Resource?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId && r.IsShared == false, cancellationToken);
    }

    public async Task<Resource?> GetByLocationForUserAsync(string location, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Location == location && r.UserId == userId && r.IsShared == false, cancellationToken);
    }

    public async Task<Resource?> GetByFilenameForUserAsync(string filename, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.FileName == filename && r.UserId == userId && r.IsShared == false, cancellationToken);
    }

    public async Task<IEnumerable<Resource>> GetAllForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.IsShared == false)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Resource?> GetSharedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.IsShared && (r.UserId == null || r.UserId == string.Empty), cancellationToken);
    }

    public async Task<Resource?> GetSharedByLocationAsync(string location, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Location == location && r.IsShared && (r.UserId == null || r.UserId == string.Empty), cancellationToken);
    }

    public async Task<Resource?> GetSharedByFilenameAsync(string filename, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.FileName == filename && r.IsShared && (r.UserId == null || r.UserId == string.Empty), cancellationToken);
    }

    public async Task<IEnumerable<Resource>> GetAllSharedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.IsShared && (r.UserId == null || r.UserId == string.Empty))
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .CountAsync(r => r.UserId == userId && r.IsShared == false, cancellationToken);
    }

    public async Task<int> CountSharedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .CountAsync(r => r.IsShared && (r.UserId == null || r.UserId == string.Empty), cancellationToken);
    }

    public async Task<IEnumerable<Resource>> GetPagedForUserAsync(string userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.IsShared == false)
            .OrderByDescending(r => r.UpdatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}

