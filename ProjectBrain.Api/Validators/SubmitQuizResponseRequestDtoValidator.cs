using FluentValidation;
using ProjectBrain.Shared.Dtos.Quizzes;

namespace ProjectBrain.Api.Validators;

/// <summary>
/// Validator for SubmitQuizResponseRequestDto
/// </summary>
public class SubmitQuizResponseRequestDtoValidator : AbstractValidator<SubmitQuizResponseRequestDto>
{
    public SubmitQuizResponseRequestDtoValidator()
    {
        RuleFor(x => x.Answers)
            .NotEmpty()
            .WithMessage("Answers are required")
            .Must(answers => answers != null && answers.Count > 0)
            .WithMessage("At least one answer must be provided");
    }
}

