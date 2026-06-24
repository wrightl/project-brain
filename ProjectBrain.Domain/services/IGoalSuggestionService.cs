namespace ProjectBrain.Domain;

public interface IGoalSuggestionService
{
    Task<GoalSuggestionResult> SuggestDailyGoalsAsync(
        string userId,
        string userName,
        CancellationToken cancellationToken = default);
}

public sealed class GoalSuggestionResult
{
    public required IReadOnlyList<string> Goals { get; init; }
    public required string Source { get; init; }
}
