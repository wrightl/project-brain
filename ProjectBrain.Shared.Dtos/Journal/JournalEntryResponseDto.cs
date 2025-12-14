namespace ProjectBrain.Shared.Dtos.Journal;

using ProjectBrain.Shared.Dtos.Tags;

/// <summary>
/// DTO for journal entry in API responses
/// </summary>
public class JournalEntryResponseDto
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string Content { get; init; }
    public string? Summary { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
    public List<TagResponseDto>? Tags { get; init; }
}

