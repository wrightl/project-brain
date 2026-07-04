namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;
using ProjectBrain.Database.Models;

/// <summary>
/// Service implementation for Goal operations
/// </summary>
public class GoalService : IGoalService
{
    private const int SuggestionHistoryLookbackDays = 365;
    public const int MaxGoalsPerDay = 3;
    public const int MaxMultidayPlans = 7;
    public const int MaxFutureDays = 30;

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
        ValidateGoalsList(goals);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await CreateOrUpdateGoalsForDateAsync(userId, today, goals, cancellationToken);
    }

    public async Task<IEnumerable<Goal>> CreateOrUpdateGoalsForDateAsync(
        string userId,
        DateOnly date,
        List<string> goals,
        CancellationToken cancellationToken = default)
    {
        ValidateGoalsList(goals);
        ValidateGoalDate(date);

        await _repository.DeleteGoalsForDateAsync(userId, date, cancellationToken);

        var newGoals = new List<Goal>();
        for (var i = 0; i < MaxGoalsPerDay; i++)
        {
            var message = i < goals.Count ? goals[i] : string.Empty;
            var goal = new Goal
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Date = date,
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
        return newGoals.OrderBy(g => g.Index);
    }

    public async Task<IReadOnlyList<MultidayGoalsResult>> CreateOrUpdateGoalsForDatesAsync(
        string userId,
        IReadOnlyList<MultidayGoalPlan> dayPlans,
        CancellationToken cancellationToken = default)
    {
        if (dayPlans is null || dayPlans.Count == 0)
        {
            throw new ArgumentException("At least one day plan is required", nameof(dayPlans));
        }

        if (dayPlans.Count > MaxMultidayPlans)
        {
            throw new ArgumentException($"Cannot create goals for more than {MaxMultidayPlans} days at once", nameof(dayPlans));
        }

        if (dayPlans.Select(p => p.Date).Distinct().Count() != dayPlans.Count)
        {
            throw new ArgumentException("Each date may only appear once in dayPlans", nameof(dayPlans));
        }

        var results = new List<MultidayGoalsResult>();

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        foreach (var plan in dayPlans.OrderBy(p => p.Date))
        {
            ValidateGoalsList(plan.Goals);
            ValidateGoalDate(plan.Date);

            var goals = await CreateOrUpdateGoalsForDateAsync(userId, plan.Date, plan.Goals, cancellationToken);
            results.Add(new MultidayGoalsResult
            {
                Date = plan.Date,
                Goals = goals.Select(g => new GoalSummary
                {
                    Index = g.Index,
                    Message = g.Message,
                    Completed = g.Completed
                }).ToList()
            });
        }

        await _unitOfWork.CommitTransactionAsync(cancellationToken);
        return results;
    }

    private static void ValidateGoalsList(List<string> goals)
    {
        if (goals == null || goals.Count == 0 || goals.Count > MaxGoalsPerDay)
        {
            throw new ArgumentException($"Goals must contain between 1 and {MaxGoalsPerDay} items", nameof(goals));
        }
    }

    private static void ValidateGoalDate(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var maxDate = today.AddDays(MaxFutureDays);

        if (date < today)
        {
            throw new ArgumentException("Goal dates cannot be in the past");
        }

        if (date > maxDate)
        {
            throw new ArgumentException($"Goal dates cannot be more than {MaxFutureDays} days in the future");
        }
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

    public async Task<int> GetLongestCompletionStreakAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetLongestCompletionStreakAsync(userId, cancellationToken);
    }

    public async Task<bool> HasEverCreatedGoalsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _repository.HasEverCreatedGoalsAsync(userId, cancellationToken);
    }

    public async Task<IReadOnlyList<IncompleteGoalBacklogItem>> GetPrioritizedIncompleteGoalBacklogAsync(
        string userId,
        int maxItems = 15,
        CancellationToken cancellationToken = default)
    {
        if (maxItems < 1)
        {
            maxItems = 1;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddDays(-SuggestionHistoryLookbackDays);

        var rows = await _repository.GetHistoricalIncompleteGoalsAsync(
            userId,
            today,
            from,
            cancellationToken);

        var aggregated = rows
            .GroupBy(g => g.Message.Trim().ToLowerInvariant())
            .Select(grp =>
            {
                var ordered = grp.OrderByDescending(x => x.Date).ThenByDescending(x => x.UpdatedAt).ToList();
                var representative = ordered[0].Message.Trim();
                return new IncompleteGoalBacklogItem(
                    string.IsNullOrEmpty(representative) ? ordered[0].Message : representative,
                    ordered.Count,
                    ordered.Max(x => x.Date));
            })
            .OrderByDescending(x => x.LastMissedDate)
            .ThenByDescending(x => x.MissCount)
            .Take(maxItems)
            .ToList();

        return aggregated;
    }
}
