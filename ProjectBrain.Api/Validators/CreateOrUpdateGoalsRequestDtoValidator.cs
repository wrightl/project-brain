using FluentValidation;
using ProjectBrain.Shared.Dtos.Goals;

namespace ProjectBrain.Api.Validators;

/// <summary>
/// Validator for CreateOrUpdateGoalsRequestDto
/// </summary>
public class CreateOrUpdateGoalsRequestDtoValidator : AbstractValidator<CreateOrUpdateGoalsRequestDto>
{
    public CreateOrUpdateGoalsRequestDtoValidator()
    {
        RuleFor(x => x.Goals)
            .NotEmpty()
            .WithMessage("Goals array is required")
            .Must(goals => goals != null && goals.Count >= 1 && goals.Count <= 3)
            .WithMessage("Goals must contain between 1 and 3 items");

        RuleForEach(x => x.Goals)
            .MaximumLength(500)
            .WithMessage("Each goal message must not exceed 500 characters");
    }
}
