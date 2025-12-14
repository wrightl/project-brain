namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository implementation for JournalEntry entity
/// </summary>
public class JournalEntryRepository : Repository<JournalEntry, Guid>, IJournalEntryRepository
{
    public JournalEntryRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<JournalEntry?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(je => je.Id == id && je.UserId == userId, cancellationToken);
    }

    public async Task<JournalEntry?> GetByIdWithTagsForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(je => je.JournalEntryTags)
                .ThenInclude(jet => jet.Tag)
            .FirstOrDefaultAsync(je => je.Id == id && je.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<JournalEntry>> GetAllForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(je => je.JournalEntryTags)
                .ThenInclude(jet => jet.Tag)
            .Where(je => je.UserId == userId)
            .OrderByDescending(je => je.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<JournalEntry>> GetPagedForUserAsync(string userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(je => je.JournalEntryTags)
                .ThenInclude(jet => jet.Tag)
            .Where(je => je.UserId == userId)
            .OrderByDescending(je => je.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<JournalEntry>> GetRecentForUserAsync(string userId, int count, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(je => je.JournalEntryTags)
                .ThenInclude(jet => jet.Tag)
            .Where(je => je.UserId == userId)
            .OrderByDescending(je => je.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .CountAsync(je => je.UserId == userId, cancellationToken);
    }
}

