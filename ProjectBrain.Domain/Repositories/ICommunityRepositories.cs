namespace ProjectBrain.Domain.Repositories;

using ProjectBrain.Database.Models;

public interface ICommunityChannelRepository : IRepository<CommunityChannel, Guid>
{
    Task<CommunityChannel?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommunityChannel>> GetActiveOrderedAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}

public interface ICommunityPostRepository : IRepository<CommunityPost, Guid>
{
    Task<CommunityPost?> GetByIdWithAuthorAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<CommunityPost> Items, bool HasMore)> GetPagedByChannelAsync(
        Guid channelId,
        DateTime? cursorCreatedAt,
        Guid? cursorId,
        int limit,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<CommunityPost> Items, bool HasMore)> GetPagedLatestAsync(
        DateTime? cursorCreatedAt,
        Guid? cursorId,
        int limit,
        CancellationToken cancellationToken = default);
}

public interface ICommunityReactionRepository : IRepository<CommunityReaction, Guid>
{
    Task<CommunityReaction?> GetByPostAndUserAsync(Guid postId, string userId, CancellationToken cancellationToken = default);
    Task<int> CountByPostAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, int>> CountByPostIdsAsync(IEnumerable<Guid> postIds, CancellationToken cancellationToken = default);
    Task<HashSet<Guid>> GetReactedPostIdsAsync(string userId, IEnumerable<Guid> postIds, CancellationToken cancellationToken = default);
}

public interface ICommunityReportRepository : IRepository<CommunityReport, Guid>
{
    Task<IReadOnlyList<CommunityReport>> GetOpenWithDetailsAsync(CancellationToken cancellationToken = default);
}
