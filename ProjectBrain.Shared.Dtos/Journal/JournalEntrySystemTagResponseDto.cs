namespace ProjectBrain.Shared.Dtos.Journal;

using System.Text.Json;

public class JournalEntrySystemTagResponseDto
{
    public required string Id { get; init; }
    public required string Key { get; init; }
    public required string Name { get; init; }
    public Dictionary<string, JsonElement>? Responses { get; init; }
}

