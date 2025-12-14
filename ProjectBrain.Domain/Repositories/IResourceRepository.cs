namespace ProjectBrain.Domain.Repositories;

/// <summary>
/// Repository interface for Resource entity with domain-specific queries
/// </summary>
public interface IResourceRepository : IRepository<Resource, Guid>
{
    /// <summary>
    /// Gets a resource by ID for a specific user
    /// </summary>
    Task<Resource?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a resource by location for a specific user
    /// </summary>
    Task<Resource?> GetByLocationForUserAsync(string location, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a resource by filename for a specific user
    /// </summary>
    Task<Resource?> GetByFilenameForUserAsync(string filename, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all resources for a user
    /// </summary>
    Task<IEnumerable<Resource>> GetAllForUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a shared resource by ID
    /// </summary>
    Task<Resource?> GetSharedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a shared resource by location
    /// </summary>
    Task<Resource?> GetSharedByLocationAsync(string location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a shared resource by filename
    /// </summary>
    Task<Resource?> GetSharedByFilenameAsync(string filename, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all shared resources
    /// </summary>
    Task<IEnumerable<Resource>> GetAllSharedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts resources for a user
    /// </summary>
    Task<int> CountForUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts shared resources
    /// </summary>
    Task<int> CountSharedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated resources for a user (efficient database-level pagination)
    /// </summary>
    Task<IEnumerable<Resource>> GetPagedForUserAsync(string userId, int skip, int take, CancellationToken cancellationToken = default);
}

