namespace ProjectBrain.Shared.Dtos.Goals;

/// <summary>
/// DTO for goal in API responses
/// </summary>
public class GoalResponseDto
{
    public required string Id { get; init; }
    public required int Index { get; init; }
    public required string Message { get; init; }
    public required bool Completed { get; init; }
    public string? CompletedAt { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
}

