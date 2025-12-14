using FluentValidation;
using ProjectBrain.Shared.Dtos.VoiceNotes;

namespace ProjectBrain.Api.Validators;

/// <summary>
/// Validator for CreateVoiceNoteRequestDto
/// </summary>
public class CreateVoiceNoteRequestDtoValidator : AbstractValidator<CreateVoiceNoteRequestDto>
{
    public CreateVoiceNoteRequestDtoValidator()
    {
        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description must not exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

