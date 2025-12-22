using FluentValidation;
using ProjectBrain.Shared.Dtos.Goals;

namespace ProjectBrain.Api.Validators;

/// <summary>
/// Validator for CompleteGoalRequestDto
/// </summary>
public class CompleteGoalRequestDtoValidator : AbstractValidator<CompleteGoalRequestDto>
{
    public CompleteGoalRequestDtoValidator()
    {
        RuleFor(x => x.Completed)
            .NotNull()
            .WithMessage("Completed field is required");
    }
}
