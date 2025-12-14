using System.ComponentModel.DataAnnotations;

namespace ProjectBrain.Shared.Dtos.CoachRatings;

/// <summary>
/// DTO for creating or updating a coach rating
/// </summary>
public class CreateCoachRatingRequestDto
{
    [Range(1, 5)]
    public required int Rating { get; init; }
    public string? Feedback { get; init; }
}

