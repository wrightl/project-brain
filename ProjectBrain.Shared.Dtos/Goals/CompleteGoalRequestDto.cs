namespace ProjectBrain.Shared.Dtos.Goals;

/// <summary>
/// DTO for completing or uncompleting a goal
/// </summary>
public class CompleteGoalRequestDto
{
    public required bool Completed { get; init; }
}

