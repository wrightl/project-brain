namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;
using ProjectBrain.Database.Models;

/// <summary>
/// Service implementation for Goal operations
/// </summary>
public class GoalService : IGoalService
{
    private readonly IGoalRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public GoalService(
        IGoalRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<IEnumerable<Goal>> GetTodaysGoalsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetTodaysGoalsAsync(userId, cancellationToken);
    }

    public async Task<IEnumerable<Goal>> CreateOrUpdateGoalsAsync(string userId, List<string> goals, CancellationToken cancellationToken = default)
    {
        if (goals == null || goals.Count == 0 || goals.Count > 3)
        {
            throw new ArgumentException("Goals must contain between 1 and 3 items", nameof(goals));
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Delete existing goals for today
        await _repository.DeleteGoalsForDateAsync(userId, today, cancellationToken);

        // Create new goals
        var newGoals = new List<Goal>();
        for (int i = 0; i < 3; i++)
        {
            var message = i < goals.Count ? goals[i] : string.Empty;
            var goal = new Goal
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Date = today,
                Index = i,
                Message = message ?? string.Empty,
                Completed = false,
                CompletedAt = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _repository.Add(goal);
            newGoals.Add(goal);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return goals ordered by index
        return newGoals.OrderBy(g => g.Index);
    }

    public async Task<IEnumerable<Goal>> CompleteGoalAsync(string userId, int index, bool completed, CancellationToken cancellationToken = default)
    {
        if (index < 0 || index > 2)
        {
            throw new ArgumentException("Index must be between 0 and 2", nameof(index));
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var goals = await _repository.GetGoalsByDateAsync(userId, today, cancellationToken);
        var goal = goals.FirstOrDefault(g => g.Index == index);

        if (goal == null || string.IsNullOrWhiteSpace(goal.Message))
        {
            throw new InvalidOperationException($"Goal at index {index} does not exist for today");
        }

        // Need to get the tracked entity for update
        var trackedGoal = await _repository.GetByIdAsync(goal.Id, cancellationToken);
        if (trackedGoal == null)
        {
            throw new InvalidOperationException($"Goal at index {index} not found");
        }

        trackedGoal.Completed = completed;
        trackedGoal.CompletedAt = completed ? DateTime.UtcNow : null;
        trackedGoal.UpdatedAt = DateTime.UtcNow;

        _repository.Update(trackedGoal);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return all goals for today
        return await _repository.GetTodaysGoalsAsync(userId, cancellationToken);
    }

    public async Task<int> GetCompletionStreakAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetCompletionStreakAsync(userId, cancellationToken);
    }

    public async Task<bool> HasEverCreatedGoalsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _repository.HasEverCreatedGoalsAsync(userId, cancellationToken);
    }
}
