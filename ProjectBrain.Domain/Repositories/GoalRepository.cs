namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;
using ProjectBrain.Database.Models;

/// <summary>
/// Repository implementation for Goal entity
/// </summary>
public class GoalRepository : Repository<Goal, Guid>, IGoalRepository
{
    public GoalRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Goal>> GetTodaysGoalsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await GetGoalsByDateAsync(userId, today, cancellationToken);
    }

    public async Task<IEnumerable<Goal>> GetGoalsByDateAsync(string userId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var goals = await _dbSet
            .AsNoTracking()
            .Where(g => g.UserId == userId && g.Date == date)
            .OrderBy(g => g.Index)
            .ToListAsync(cancellationToken);

        // Always return 3 goals, pad with empty goals if needed
        var result = new List<Goal>();
        for (int i = 0; i < 3; i++)
        {
            var existingGoal = goals.FirstOrDefault(g => g.Index == i);
            if (existingGoal != null)
            {
                result.Add(existingGoal);
            }
            else
            {
                // Create placeholder goal
                result.Add(new Goal
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Date = date,
                    Index = i,
                    Message = string.Empty,
                    Completed = false,
                    CompletedAt = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        return result;
    }

    public async Task DeleteGoalsForDateAsync(string userId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var goals = await _dbSet
            .Where(g => g.UserId == userId && g.Date == date)
            .ToListAsync(cancellationToken);

        if (goals.Any())
        {
            _dbSet.RemoveRange(goals);
        }
    }

    public async Task<int> GetCompletionStreakAsync(string userId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var streak = 0;
        var currentDate = today;

        while (true)
        {
            // Get goals for this date
            var goals = await _dbSet
                .AsNoTracking()
                .Where(g => g.UserId == userId && g.Date == currentDate)
                .ToListAsync(cancellationToken);

            // Filter to only goals with non-empty messages
            var nonEmptyGoals = goals.Where(g => !string.IsNullOrWhiteSpace(g.Message)).ToList();

            // Check if we have at least one goal and all non-empty goals are completed
            if (nonEmptyGoals.Count > 0 && nonEmptyGoals.All(g => g.Completed))
            {
                streak++;
                // Move to previous day
                currentDate = currentDate.AddDays(-1);
            }
            else
            {
                // Streak broken - no goals for this day or not all completed
                break;
            }
        }

        return streak;
    }

    public async Task<bool> HasEverCreatedGoalsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .AnyAsync(g => g.UserId == userId && !string.IsNullOrWhiteSpace(g.Message), cancellationToken);
    }
}
