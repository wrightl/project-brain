namespace ProjectBrain.Shared.Dtos.CoachRatings;

/// <summary>
/// DTO for coach rating response
/// </summary>
public class CoachRatingResponseDto
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string CoachId { get; init; }
    public required string UserName { get; init; }
    public required int Rating { get; init; }
    public string? Feedback { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}

