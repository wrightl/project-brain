namespace ProjectBrain.Shared.Dtos.SystemTags;

public class SystemTagFieldDefinitionResponseDto
{
    public required string Id { get; init; }
    public required string FieldKey { get; init; }
    public required string Label { get; init; }
    public required string InputType { get; init; }
    public bool Required { get; init; }
    public int FieldOrder { get; init; }
    public string? Placeholder { get; init; }
    public string? Hint { get; init; }
    public List<string>? Options { get; init; }
    public decimal? MinValue { get; init; }
    public decimal? MaxValue { get; init; }
    public decimal? StepValue { get; init; }
}

