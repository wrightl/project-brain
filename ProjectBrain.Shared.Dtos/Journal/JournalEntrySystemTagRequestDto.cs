namespace ProjectBrain.Shared.Dtos.Journal;

using System.Text.Json;

public class JournalEntrySystemTagRequestDto
{
    public required Guid SystemTagId { get; init; }
    public Dictionary<string, JsonElement>? Responses { get; init; }
}

