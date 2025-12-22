namespace ProjectBrain.Shared.Dtos.Goals;

/// <summary>
/// DTO for creating or updating daily goals
/// </summary>
public class CreateOrUpdateGoalsRequestDto
{
    public required List<string> Goals { get; init; }
}

