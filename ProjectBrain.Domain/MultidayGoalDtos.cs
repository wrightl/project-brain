namespace ProjectBrain.Domain;

public sealed class MultidayGoalPlan
{
    public required DateOnly Date { get; init; }
    public required List<string> Goals { get; init; }
}

public sealed class MultidayGoalsResult
{
    public required DateOnly Date { get; init; }
    public required IReadOnlyList<GoalSummary> Goals { get; init; }
}

public sealed class GoalSummary
{
    public int Index { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool Completed { get; init; }
}
