namespace ProjectBrain.Domain.Mappers;

using ProjectBrain.Shared.Dtos.Journal;
using ProjectBrain.Shared.Dtos.Tags;

public static class JournalEntryMapper
{
    public static JournalEntryResponseDto ToDto(JournalEntry journalEntry)
    {
        var tags = journalEntry.JournalEntryTags?
            .Select(jet => jet.Tag)
            .Where(t => t != null)
            .Select(t => new TagResponseDto
            {
                Id = t!.Id.ToString(),
                Name = t.Name,
                CreatedAt = t.CreatedAt.ToString("O")
            })
            .ToList();

        return new JournalEntryResponseDto
        {
            Id = journalEntry.Id.ToString(),
            UserId = journalEntry.UserId,
            Content = journalEntry.Content,
            Summary = journalEntry.Summary,
            CreatedAt = journalEntry.CreatedAt.ToString("O"),
            UpdatedAt = journalEntry.UpdatedAt.ToString("O"),
            Tags = tags
        };
    }

    public static List<JournalEntryResponseDto> ToDtoList(IEnumerable<JournalEntry> journalEntries)
    {
        return journalEntries.Select(ToDto).ToList();
    }
}

