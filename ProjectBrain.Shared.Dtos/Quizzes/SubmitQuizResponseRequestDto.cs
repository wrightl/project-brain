namespace ProjectBrain.Shared.Dtos.Quizzes;

/// <summary>
/// DTO for submitting a quiz response
/// </summary>
public class SubmitQuizResponseRequestDto
{
    public required Dictionary<string, object> Answers { get; init; }
    public DateTime? CompletedAt { get; init; }
}

