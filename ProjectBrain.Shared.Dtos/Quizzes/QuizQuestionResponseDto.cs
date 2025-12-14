namespace ProjectBrain.Shared.Dtos.Quizzes;

/// <summary>
/// DTO for quiz question in API responses
/// </summary>
public class QuizQuestionResponseDto
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required string InputType { get; init; }
    public bool Mandatory { get; init; }
    public bool Visible { get; init; }
    public decimal? MinValue { get; init; }
    public decimal? MaxValue { get; init; }
    public List<string>? Choices { get; init; }
    public string? Placeholder { get; init; }
    public string? Hint { get; init; }
}

