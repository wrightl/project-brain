namespace ProjectBrain.Domain.Mappers;

using ProjectBrain.Database.Models;
using ProjectBrain.Shared.Dtos.CoachRatings;

public static class CoachRatingMapper
{
    public static CoachRatingResponseDto ToDto(this CoachRating rating)
    {
        return new CoachRatingResponseDto
        {
            Id = rating.Id.ToString(),
            UserId = rating.UserId,
            CoachId = rating.CoachId,
            UserName = rating.User?.FullName ?? "Unknown",
            Rating = rating.Rating,
            Feedback = rating.Feedback,
            CreatedAt = rating.CreatedAt,
            UpdatedAt = rating.UpdatedAt
        };
    }

    public static List<CoachRatingResponseDto> ToDtoList(this IEnumerable<CoachRating> ratings)
    {
        return ratings.Select(r => r.ToDto()).ToList();
    }
}

