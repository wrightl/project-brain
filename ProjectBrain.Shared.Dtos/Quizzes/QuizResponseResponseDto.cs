namespace ProjectBrain.Shared.Dtos.Quizzes;

/// <summary>
/// DTO for quiz response in API responses
/// </summary>
public class QuizResponseResponseDto
{
    public required string Id { get; init; }
    public required string QuizId { get; init; }
    public string? QuizTitle { get; init; }
    public required string UserId { get; init; }
    public Dictionary<string, object>? Answers { get; init; }
    public decimal? Score { get; init; }
    public required string CompletedAt { get; init; }
    public required string CreatedAt { get; init; }
}

