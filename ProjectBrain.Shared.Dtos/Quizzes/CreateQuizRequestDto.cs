namespace ProjectBrain.Shared.Dtos.Quizzes;

/// <summary>
/// DTO for creating or updating a quiz
/// </summary>
public class CreateQuizRequestDto
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required List<CreateQuestionRequestDto> Questions { get; init; }
}

/// <summary>
/// DTO for creating or updating a quiz question
/// </summary>
public class CreateQuestionRequestDto
{
    public string? Id { get; init; }
    public required string Label { get; init; }
    public required string InputType { get; init; }
    public bool Mandatory { get; init; } = false;
    public bool Visible { get; init; } = true;
    public decimal? MinValue { get; init; }
    public decimal? MaxValue { get; init; }
    public List<string>? Choices { get; init; }
    public string? Placeholder { get; init; }
    public string? Hint { get; init; }
}

