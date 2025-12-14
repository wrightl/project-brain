namespace ProjectBrain.Shared.Dtos.Tags;

/// <summary>
/// DTO for tag in API responses
/// </summary>
public class TagResponseDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string CreatedAt { get; init; }
}

