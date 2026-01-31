namespace ProjectBrain.Shared.Dtos.Achievements;

public class AchievementItemDto
{
    public required string Id { get; init; }
    public required string Key { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public string? IconKey { get; init; }
    public DateTime? EarnedAt { get; init; }
}

