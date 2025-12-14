namespace ProjectBrain.Domain.Repositories;

using ProjectBrain.Database.Models;

/// <summary>
/// Repository interface for CoachProfile entity with domain-specific queries
/// </summary>
public interface ICoachProfileRepository : IRepository<CoachProfile, int>
{
    /// <summary>
    /// Gets a coach profile by ID
    /// </summary>
    new Task<CoachProfile?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a coach profile by ID with related entities
    /// </summary>
    Task<CoachProfile?> GetByIdWithRelatedAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a coach profile by ID
    /// </summary>
    Task<CoachProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a coach profile by user ID with related entities
    /// </summary>
    Task<CoachProfile?> GetByUserIdWithRelatedAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for coach profiles with filters
    /// </summary>
    Task<IEnumerable<CoachProfile>> SearchAsync(
        string? city = null,
        string? stateProvince = null,
        string? country = null,
        IEnumerable<string>? ageGroups = null,
        IEnumerable<string>? specialisms = null,
        CancellationToken cancellationToken = default);
}

