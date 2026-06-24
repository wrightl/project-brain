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
    /// Creates or updates goals for a specific date (always 3 goal slots).
    /// </summary>
    Task<IEnumerable<Goal>> CreateOrUpdateGoalsForDateAsync(
        string userId,
        DateOnly date,
        List<string> goals,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates goals for multiple dates in one operation.
    /// </summary>
    Task<IReadOnlyList<MultidayGoalsResult>> CreateOrUpdateGoalsForDatesAsync(
        string userId,
        IReadOnlyList<MultidayGoalPlan> dayPlans,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes or uncompletes a goal at the specified index
    /// </summary>
    Task<IEnumerable<Goal>> CompleteGoalAsync(string userId, int index, bool completed, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the completion streak (consecutive days with all goals completed)
    /// </summary>
    Task<int> GetCompletionStreakAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the longest completion streak (max consecutive days with all non-empty goals completed)
    /// </summary>
    Task<int> GetLongestCompletionStreakAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if user has ever created any goals
    /// </summary>
    Task<bool> HasEverCreatedGoalsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prior incomplete goals grouped by normalized text: most recently missed first, then by miss count.
    /// </summary>
    Task<IReadOnlyList<IncompleteGoalBacklogItem>> GetPrioritizedIncompleteGoalBacklogAsync(
        string userId,
        int maxItems = 15,
        CancellationToken cancellationToken = default);
}
