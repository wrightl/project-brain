namespace ProjectBrain.Domain.Repositories;

/// <summary>
/// Repository interface for User entity with domain-specific queries
/// </summary>
public interface IUserRepository : IRepository<User, string>
{
    /// <summary>
    /// Gets a user by email address
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by ID with roles
    /// </summary>
    Task<User?> GetByIdWithRolesAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email with roles
    /// </summary>
    Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated users with roles (efficient database-level pagination)
    /// </summary>
    Task<IEnumerable<User>> GetPagedWithRolesAsync(int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts all users
    /// </summary>
    Task<int> CountAllAsync(CancellationToken cancellationToken = default);
}

