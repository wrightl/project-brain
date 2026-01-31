namespace ProjectBrain.Domain.Mappers;

using ProjectBrain.Shared.Dtos.Journal;
using ProjectBrain.Shared.Dtos.Tags;
using System.Text.Json;

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

        var systemTags = journalEntry.JournalEntrySystemTags?
            .Select(jest => new { jest.SystemTag, jest.ResponsesJson })
            .Where(x => x.SystemTag != null)
            .Select(x =>
            {
                Dictionary<string, JsonElement>? responses = null;
                if (!string.IsNullOrWhiteSpace(x.ResponsesJson))
                {
                    try
                    {
                        responses = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(x.ResponsesJson!);
                    }
                    catch
                    {
                        // Ignore parse errors; responses will be omitted
                        responses = null;
                    }
                }

                return new JournalEntrySystemTagResponseDto
                {
                    Id = x.SystemTag!.Id.ToString(),
                    Key = x.SystemTag.Key,
                    Name = x.SystemTag.Name,
                    Responses = responses
                };
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
            Tags = tags,
            SystemTags = systemTags
        };
    }

    public static List<JournalEntryResponseDto> ToDtoList(IEnumerable<JournalEntry> journalEntries)
    {
        return journalEntries.Select(ToDto).ToList();
    }
}

