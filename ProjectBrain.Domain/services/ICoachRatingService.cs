namespace ProjectBrain.Domain;

using ProjectBrain.Database.Models;

public interface ICoachRatingService
{
    /// <summary>
    /// Creates or updates a rating for a coach by a user
    /// </summary>
    Task<CoachRating> CreateOrUpdateRatingAsync(string userId, string coachId, int rating, string? feedback = null);

    /// <summary>
    /// Gets a rating by user ID and coach ID
    /// </summary>
    Task<CoachRating?> GetRatingAsync(string userId, string coachId);

    /// <summary>
    /// Gets all ratings for a coach
    /// </summary>
    Task<IEnumerable<CoachRating>> GetRatingsByCoachIdAsync(string coachId);

    /// <summary>
    /// Gets paginated ratings for a coach
    /// </summary>
    Task<IEnumerable<CoachRating>> GetPagedRatingsByCoachIdAsync(string coachId, int skip, int take);

    /// <summary>
    /// Gets the average rating for a coach
    /// </summary>
    Task<double?> GetAverageRatingAsync(string coachId);

    /// <summary>
    /// Gets the count of ratings for a coach
    /// </summary>
    Task<int> GetRatingCountAsync(string coachId);
}

