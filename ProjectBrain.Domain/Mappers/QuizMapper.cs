namespace ProjectBrain.Domain.Mappers;

using ProjectBrain.Shared.Dtos.Quizzes;

/// <summary>
/// Mapper for converting Quiz entities to DTOs
/// </summary>
public static class QuizMapper
{
    /// <summary>
    /// Maps a Quiz entity to a QuizResponseDto
    /// </summary>
    public static QuizResponseDto ToDto(Quiz quiz, bool includeQuestions = true)
    {
        return new QuizResponseDto
        {
            Id = quiz.Id.ToString(),
            Title = quiz.Title,
            Description = quiz.Description,
            Questions = includeQuestions ? quiz.Questions.OrderBy(q => q.QuestionOrder).Select(QuizQuestionMapper.ToDto).ToList() : null,
            CreatedAt = quiz.CreatedAt.ToString("O"),
            UpdatedAt = quiz.UpdatedAt.ToString("O")
        };
    }

    /// <summary>
    /// Maps a collection of Quiz entities to DTOs
    /// </summary>
    public static IEnumerable<QuizResponseDto> ToDto(IEnumerable<Quiz> quizzes, bool includeQuestions = false)
    {
        return quizzes.Select(q => ToDto(q, includeQuestions));
    }
}

/// <summary>
/// Mapper for converting QuizQuestion entities to DTOs
/// </summary>
public static class QuizQuestionMapper
{
    /// <summary>
    /// Maps a QuizQuestion entity to a QuizQuestionResponseDto
    /// </summary>
    public static QuizQuestionResponseDto ToDto(QuizQuestion question)
    {
        return new QuizQuestionResponseDto
        {
            Id = question.Id.ToString(),
            Label = question.Label,
            InputType = question.InputType,
            Mandatory = question.Mandatory,
            Visible = question.Visible,
            MinValue = question.MinValue,
            MaxValue = question.MaxValue,
            Choices = question.Choices,
            Placeholder = question.Placeholder,
            Hint = question.Hint
        };
    }
}

/// <summary>
/// Mapper for converting QuizResponse entities to DTOs
/// </summary>
public static class QuizResponseMapper
{
    /// <summary>
    /// Maps a QuizResponse entity to a QuizResponseResponseDto
    /// </summary>
    public static QuizResponseResponseDto ToDto(QuizResponse quizResponse)
    {
        return new QuizResponseResponseDto
        {
            Id = quizResponse.Id.ToString(),
            QuizId = quizResponse.QuizId.ToString(),
            QuizTitle = quizResponse.Quiz?.Title,
            UserId = quizResponse.UserId,
            Answers = quizResponse.Answers,
            Score = quizResponse.Score,
            CompletedAt = quizResponse.CompletedAt.ToString("O"),
            CreatedAt = quizResponse.CreatedAt.ToString("O")
        };
    }

    /// <summary>
    /// Maps a collection of QuizResponse entities to DTOs
    /// </summary>
    public static IEnumerable<QuizResponseResponseDto> ToDto(IEnumerable<QuizResponse> quizResponses)
    {
        return quizResponses.Select(ToDto);
    }
}

