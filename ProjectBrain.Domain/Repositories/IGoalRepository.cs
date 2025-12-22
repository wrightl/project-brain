namespace ProjectBrain.Domain.Repositories;

using ProjectBrain.Database.Models;

/// <summary>
/// Repository interface for Goal entity with domain-specific queries
/// </summary>
public interface IGoalRepository : IRepository<Goal, Guid>
{
    /// <summary>
    /// Gets today's goals for a user (always returns 3 goals, padded with empty if needed)
    /// </summary>
    Task<IEnumerable<Goal>> GetTodaysGoalsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets goals for a specific date for a user
    /// </summary>
    Task<IEnumerable<Goal>> GetGoalsByDateAsync(string userId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all goals for a specific date for a user
    /// </summary>
    Task DeleteGoalsForDateAsync(string userId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the completion streak (consecutive days with all 3 goals completed)
    /// </summary>
    Task<int> GetCompletionStreakAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if user has ever created any goals
    /// </summary>
    Task<bool> HasEverCreatedGoalsAsync(string userId, CancellationToken cancellationToken = default);
}
