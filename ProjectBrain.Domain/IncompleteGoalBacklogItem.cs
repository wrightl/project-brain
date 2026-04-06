namespace ProjectBrain.Domain;

/// <summary>
/// Aggregated historical incomplete goal, ordered for AI suggestion context.
/// </summary>
public sealed record IncompleteGoalBacklogItem(
    string Message,
    int MissCount,
    DateOnly LastMissedDate);
