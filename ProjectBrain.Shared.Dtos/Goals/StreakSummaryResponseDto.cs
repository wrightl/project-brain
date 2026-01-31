namespace ProjectBrain.Shared.Dtos.Goals;

/// <summary>
/// DTO for streak summary response (current + longest).
/// </summary>
public class StreakSummaryResponseDto
{
    public required int CurrentStreak { get; init; }
    public required int LongestStreak { get; init; }
}

