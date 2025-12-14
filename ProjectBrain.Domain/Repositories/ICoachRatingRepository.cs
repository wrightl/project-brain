namespace ProjectBrain.Domain.Repositories;

using ProjectBrain.Database.Models;

/// <summary>
/// Repository interface for CoachRating entity with domain-specific queries
/// </summary>
public interface ICoachRatingRepository : IRepository<CoachRating, Guid>
{
    /// <summary>
    /// Gets a rating by user ID and coach ID
    /// </summary>
    Task<CoachRating?> GetByUserAndCoachAsync(string userId, string coachId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all ratings for a coach
    /// </summary>
    Task<IEnumerable<CoachRating>> GetRatingsByCoachIdAsync(string coachId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated ratings for a coach
    /// </summary>
    Task<IEnumerable<CoachRating>> GetPagedRatingsByCoachIdAsync(string coachId, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts ratings for a coach
    /// </summary>
    Task<int> CountRatingsByCoachIdAsync(string coachId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the average rating for a coach
    /// </summary>
    Task<double?> GetAverageRatingByCoachIdAsync(string coachId, CancellationToken cancellationToken = default);
}

