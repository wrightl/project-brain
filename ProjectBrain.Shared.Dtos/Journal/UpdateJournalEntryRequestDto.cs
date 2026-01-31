namespace ProjectBrain.Shared.Dtos.Journal;

/// <summary>
/// DTO for updating a journal entry
/// </summary>
public class UpdateJournalEntryRequestDto
{
    public required string Content { get; init; }
    public List<Guid>? TagIds { get; init; }
    public List<Guid>? SystemTagIds { get; init; }
    public List<JournalEntrySystemTagRequestDto>? SystemTagResponses { get; init; }
}

