namespace ProjectBrain.Shared.Dtos.SystemTags;

public class SystemTagResponseDto
{
    public required string Id { get; init; }
    public required string Key { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public List<SystemTagFieldDefinitionResponseDto> FieldDefinitions { get; init; } = new();
}

