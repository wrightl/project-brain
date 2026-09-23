namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;
using ProjectBrain.Database.Models;

public class CommunityChannelRepository : Repository<CommunityChannel, Guid>, ICommunityChannelRepository
{
    public CommunityChannelRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<CommunityChannel?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);
    }

    public async Task<IReadOnlyList<CommunityChannel>> GetActiveOrderedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _dbSet.AsNoTracking().CountAsync(cancellationToken);
}

public class CommunityPostRepository : Repository<CommunityPost, Guid>, ICommunityPostRepository
{
    public CommunityPostRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<CommunityPost?> GetByIdWithAuthorAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Author)
            .Include(p => p.Channel)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<CommunityPost> Items, bool HasMore)> GetPagedByChannelAsync(
        Guid channelId,
        DateTime? cursorCreatedAt,
        Guid? cursorId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, 50);
        var query = _dbSet.AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.Channel)
            .Where(p => p.ChannelId == channelId && p.DeletedAt == null && !p.IsHidden);

        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            var createdAt = cursorCreatedAt.Value;
            var id = cursorId.Value;
            query = query.Where(p =>
                p.CreatedAt < createdAt ||
                (p.CreatedAt == createdAt && p.Id.CompareTo(id) < 0));
        }

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Take(take + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > take;
        if (hasMore)
        {
            items = items.Take(take).ToList();
        }

        return (items, hasMore);
    }

    public async Task<(IReadOnlyList<CommunityPost> Items, bool HasMore)> GetPagedLatestAsync(
        DateTime? cursorCreatedAt,
        Guid? cursorId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, 50);
        var query = _dbSet.AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.Channel)
            .Where(p =>
                p.DeletedAt == null &&
                !p.IsHidden &&
                p.Channel != null &&
                p.Channel.IsActive);

        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            var createdAt = cursorCreatedAt.Value;
            var id = cursorId.Value;
            query = query.Where(p =>
                p.CreatedAt < createdAt ||
                (p.CreatedAt == createdAt && p.Id.CompareTo(id) < 0));
        }

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Take(take + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > take;
        if (hasMore)
        {
            items = items.Take(take).ToList();
        }

        return (items, hasMore);
    }
}

public class CommunityReactionRepository : Repository<CommunityReaction, Guid>, ICommunityReactionRepository
{
    public CommunityReactionRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<CommunityReaction?> GetByPostAndUserAsync(Guid postId, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(
            r => r.PostId == postId && r.UserId == userId,
            cancellationToken);
    }

    public Task<int> CountByPostAsync(Guid postId, CancellationToken cancellationToken = default) =>
        _dbSet.AsNoTracking().CountAsync(r => r.PostId == postId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> CountByPostIdsAsync(
        IEnumerable<Guid> postIds,
        CancellationToken cancellationToken = default)
    {
        var ids = postIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        return await _dbSet.AsNoTracking()
            .Where(r => ids.Contains(r.PostId))
            .GroupBy(r => r.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count, cancellationToken);
    }

    public async Task<HashSet<Guid>> GetReactedPostIdsAsync(
        string userId,
        IEnumerable<Guid> postIds,
        CancellationToken cancellationToken = default)
    {
        var ids = postIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var reacted = await _dbSet.AsNoTracking()
            .Where(r => r.UserId == userId && ids.Contains(r.PostId))
            .Select(r => r.PostId)
            .ToListAsync(cancellationToken);

        return reacted.ToHashSet();
    }
}

public class CommunityReportRepository : Repository<CommunityReport, Guid>, ICommunityReportRepository
{
    public CommunityReportRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<CommunityReport>> GetOpenWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Include(r => r.Reporter)
            .Include(r => r.Post)!.ThenInclude(p => p!.Channel)
            .Where(r => r.Status == CommunityReport.StatusOpen)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
