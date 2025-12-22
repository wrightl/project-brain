namespace ProjectBrain.Shared.Dtos.Goals;

/// <summary>
/// DTO for goals response with streak information
/// </summary>
public class GoalsWithStreakResponseDto
{
    public required List<GoalResponseDto> Goals { get; init; }
    public int? Streak { get; init; }
}

