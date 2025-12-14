namespace ProjectBrain.Domain.Repositories;

/// <summary>
/// Repository interface for Tag entity with domain-specific queries
/// </summary>
public interface ITagRepository : IRepository<Tag, Guid>
{
    /// <summary>
    /// Gets a tag by ID for a specific user
    /// </summary>
    Task<Tag?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tag by name for a specific user
    /// </summary>
    Task<Tag?> GetByNameForUserAsync(string name, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tags for a user
    /// </summary>
    Task<IEnumerable<Tag>> GetAllForUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tags by IDs for a specific user
    /// </summary>
    Task<IEnumerable<Tag>> GetByIdsForUserAsync(IEnumerable<Guid> tagIds, string userId, CancellationToken cancellationToken = default);
}

