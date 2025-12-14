namespace ProjectBrain.Shared.Dtos.Tags;

/// <summary>
/// DTO for creating a tag
/// </summary>
public class CreateTagRequestDto
{
    public required string Name { get; init; }
}

