namespace ProjectBrain.Shared.Dtos.VoiceNotes;

/// <summary>
/// Voice note response DTO
/// </summary>
public class VoiceNoteResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
    public decimal? Duration { get; set; }
    public long? FileSize { get; set; }
    public string? Description { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

