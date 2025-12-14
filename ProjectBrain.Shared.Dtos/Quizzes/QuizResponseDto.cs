namespace ProjectBrain.Shared.Dtos.Quizzes;

/// <summary>
/// DTO for quiz in API responses
/// </summary>
public class QuizResponseDto
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public List<QuizQuestionResponseDto>? Questions { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
}

