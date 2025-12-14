namespace ProjectBrain.Domain.Repositories;

/// <summary>
/// Repository interface for UserProfile entity with domain-specific queries
/// </summary>
public interface IUserProfileRepository : IRepository<UserProfile, int>
{
    /// <summary>
    /// Gets a user profile by user ID
    /// </summary>
    Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user profile by user ID with related entities
    /// </summary>
    Task<UserProfile?> GetByUserIdWithRelatedAsync(string userId, CancellationToken cancellationToken = default);
}

