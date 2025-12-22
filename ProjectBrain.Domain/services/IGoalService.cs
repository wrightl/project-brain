using ProjectBrain.Database.Models;

namespace ProjectBrain.Domain;

/// <summary>
/// Service interface for Goal operations
/// </summary>
public interface IGoalService
{
    /// <summary>
    /// Gets today's goals for a user (always returns 3 goals)
    /// </summary>
    Task<IEnumerable<Goal>> GetTodaysGoalsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates goals for today
    /// </summary>
    Task<IEnumerable<Goal>> CreateOrUpdateGoalsAsync(string userId, List<string> goals, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes or uncompletes a goal at the specified index
    /// </summary>
    Task<IEnumerable<Goal>> CompleteGoalAsync(string userId, int index, bool completed, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the completion streak (consecutive days with all goals completed)
    /// </summary>
    Task<int> GetCompletionStreakAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if user has ever created any goals
    /// </summary>
    Task<bool> HasEverCreatedGoalsAsync(string userId, CancellationToken cancellationToken = default);
}
