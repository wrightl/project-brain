namespace ProjectBrain.Shared.Dtos.Journal;

/// <summary>
/// DTO for creating a journal entry
/// </summary>
public class CreateJournalEntryRequestDto
{
    public required string Content { get; init; }
    public List<Guid>? TagIds { get; init; }
    public List<Guid>? SystemTagIds { get; init; }
    public List<JournalEntrySystemTagRequestDto>? SystemTagResponses { get; init; }
}

