namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository implementation for VoiceNote entity
/// </summary>
public class VoiceNoteRepository : Repository<VoiceNote, Guid>, IVoiceNoteRepository
{
    public VoiceNoteRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<VoiceNote?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(vn => vn.Id == id && vn.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<VoiceNote>> GetAllForUserAsync(string userId, int? limit = null, CancellationToken cancellationToken = default)
    {
        IQueryable<VoiceNote> query = _dbSet
            .AsNoTracking()
            .Where(vn => vn.UserId == userId)
            .OrderByDescending(vn => vn.CreatedAt);

        if (limit.HasValue && limit.Value > 0)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<VoiceNote>> GetPagedForUserAsync(string userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(vn => vn.UserId == userId)
            .OrderByDescending(vn => vn.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}

