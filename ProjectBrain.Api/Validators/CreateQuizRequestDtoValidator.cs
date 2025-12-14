using FluentValidation;
using ProjectBrain.Shared.Dtos.Quizzes;

namespace ProjectBrain.Api.Validators;

/// <summary>
/// Validator for CreateQuizRequestDto
/// </summary>
public class CreateQuizRequestDtoValidator : AbstractValidator<CreateQuizRequestDto>
{
    private static readonly string[] ValidInputTypes = { "text", "number", "email", "date", "choice", "multipleChoice", "scale", "textarea", "tel", "url" };

    public CreateQuizRequestDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Quiz title is required")
            .MaximumLength(500)
            .WithMessage("Quiz title must not exceed 500 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Quiz description must not exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Questions)
            .NotEmpty()
            .WithMessage("Quiz must have at least one question")
            .Must(questions => questions != null && questions.Count > 0)
            .WithMessage("Quiz must have at least one question");

        RuleForEach(x => x.Questions)
            .SetValidator(new CreateQuestionRequestDtoValidator());
    }
}

/// <summary>
/// Validator for CreateQuestionRequestDto
/// </summary>
public class CreateQuestionRequestDtoValidator : AbstractValidator<CreateQuestionRequestDto>
{
    private static readonly string[] ValidInputTypes = { "text", "number", "email", "date", "choice", "multipleChoice", "scale", "textarea", "tel", "url" };

    public CreateQuestionRequestDtoValidator()
    {
        RuleFor(x => x.Label)
            .NotEmpty()
            .WithMessage("Question label is required")
            .MaximumLength(1000)
            .WithMessage("Question label must not exceed 1000 characters");

        RuleFor(x => x.InputType)
            .NotEmpty()
            .WithMessage("Question input type is required")
            .Must(inputType => ValidInputTypes.Contains(inputType))
            .WithMessage($"Question input type must be one of: {string.Join(", ", ValidInputTypes)}");

        RuleFor(x => x.Choices)
            .NotEmpty()
            .WithMessage("Choices are required for choice and multipleChoice input types")
            .When(x => x.InputType == "choice" || x.InputType == "multipleChoice");

        RuleFor(x => x.MinValue)
            .LessThanOrEqualTo(x => x.MaxValue)
            .WithMessage("Minimum value must be less than or equal to maximum value")
            .When(x => x.MinValue.HasValue && x.MaxValue.HasValue);

        RuleFor(x => x.Placeholder)
            .MaximumLength(255)
            .WithMessage("Placeholder must not exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.Placeholder));

        RuleFor(x => x.Hint)
            .MaximumLength(500)
            .WithMessage("Hint must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Hint));
    }
}

