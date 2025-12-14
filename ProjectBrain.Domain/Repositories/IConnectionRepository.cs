namespace ProjectBrain.Domain.Repositories;

/// <summary>
/// Repository interface for Connection entity with domain-specific queries
/// </summary>
public interface IConnectionRepository : IRepository<Connection, Guid>
{
    /// <summary>
    /// Gets a connection by user ID and coach ID
    /// </summary>
    Task<Connection?> GetByUserAndCoachAsync(string userId, string coachId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all connections for a user or coach
    /// </summary>
    Task<IEnumerable<Connection>> GetConnectionsAsync(string userId, bool isCoach, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all connected coach IDs for a user (accepted or pending)
    /// </summary>
    Task<IEnumerable<Connection>> GetConnectedCoachesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all connected user IDs for a coach (accepted or pending)
    /// </summary>
    Task<IEnumerable<Connection>> GetConnectedUsersAsync(string coachId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all connections by coach ID (accepted or pending)
    /// </summary>
    Task<IEnumerable<Connection>> GetConnectionsByCoachIdAsync(string coachId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the earliest connection date for a user
    /// </summary>
    Task<DateTime?> GetEarliestConnectionDateAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts connections for a user or coach
    /// </summary>
    Task<int> CountConnectionsAsync(string userId, bool isCoach, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated connections for a user or coach (efficient database-level pagination)
    /// </summary>
    Task<IEnumerable<Connection>> GetPagedConnectionsAsync(string userId, bool isCoach, int skip, int take, CancellationToken cancellationToken = default);
}

