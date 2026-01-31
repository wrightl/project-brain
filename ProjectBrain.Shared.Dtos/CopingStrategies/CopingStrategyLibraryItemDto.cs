namespace ProjectBrain.Shared.Dtos.CopingStrategies;

public class CopingStrategyLibraryItemDto
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public string? IconKey { get; init; }
    public int? Rating { get; init; }
    public required DateTime SavedAt { get; init; }
}

