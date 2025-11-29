using System.ComponentModel.DataAnnotations;

public class VoiceNote
{
    public Guid Id { get; set; }

    [StringLength(128)]
    public required string UserId { get; set; }

    [StringLength(255)]
    public required string FileName { get; set; }

    [StringLength(512)]
    public required string FilePath { get; set; }

    [StringLength(512)]
    public required string AudioUrl { get; set; }

    public decimal? Duration { get; set; } // Duration in seconds

    public long? FileSize { get; set; } // File size in bytes

    [StringLength(100)]
    public string MimeType { get; set; } = "audio/m4a";

    [StringLength(1000)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

