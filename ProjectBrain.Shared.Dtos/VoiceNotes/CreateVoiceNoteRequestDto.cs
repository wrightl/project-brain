namespace ProjectBrain.Shared.Dtos.VoiceNotes;

/// <summary>
/// Request DTO for creating a voice note (used for validation)
/// Note: File upload is handled via multipart/form-data, not JSON
/// </summary>
public class CreateVoiceNoteRequestDto
{
    /// <summary>
    /// Optional description for the voice note
    /// </summary>
    public string? Description { get; set; }
}

