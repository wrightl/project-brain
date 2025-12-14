namespace ProjectBrain.Domain.Mappers;

using ProjectBrain.Shared.Dtos.VoiceNotes;

/// <summary>
/// Mapper for converting VoiceNote entities to DTOs
/// </summary>
public static class VoiceNoteMapper
{
    /// <summary>
    /// Maps a VoiceNote entity to a VoiceNoteResponseDto
    /// </summary>
    public static VoiceNoteResponseDto ToDto(VoiceNote voiceNote)
    {
        return new VoiceNoteResponseDto
        {
            Id = voiceNote.Id.ToString(),
            FileName = voiceNote.FileName,
            AudioUrl = voiceNote.AudioUrl,
            Duration = voiceNote.Duration,
            FileSize = voiceNote.FileSize,
            Description = voiceNote.Description,
            CreatedAt = voiceNote.CreatedAt.ToString("O"),
            UpdatedAt = voiceNote.UpdatedAt.ToString("O")
        };
    }

    /// <summary>
    /// Maps a collection of VoiceNote entities to DTOs
    /// </summary>
    public static IEnumerable<VoiceNoteResponseDto> ToDto(IEnumerable<VoiceNote> voiceNotes)
    {
        return voiceNotes.Select(ToDto);
    }
}

