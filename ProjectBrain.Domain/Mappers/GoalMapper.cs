namespace ProjectBrain.Domain.Mappers;

using ProjectBrain.Shared.Dtos.Goals;
using ProjectBrain.Database.Models;

public static class GoalMapper
{
    public static GoalResponseDto ToDto(Goal goal)
    {
        return new GoalResponseDto
        {
            Id = goal.Id.ToString(),
            Index = goal.Index,
            Message = goal.Message,
            Completed = goal.Completed,
            CompletedAt = goal.CompletedAt?.ToString("O"),
            CreatedAt = goal.CreatedAt.ToString("O"),
            UpdatedAt = goal.UpdatedAt.ToString("O")
        };
    }

    public static List<GoalResponseDto> ToDtoList(IEnumerable<Goal> goals)
    {
        return goals.Select(ToDto).ToList();
    }
}

