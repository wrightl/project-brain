using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class CoachMessage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(128)]
    public required string UserId { get; set; }

    [Required]
    [StringLength(128)]
    public required string CoachId { get; set; }

    [Required]
    public Guid ConnectionId { get; set; }

    [Required]
    [StringLength(128)]
    public required string SenderId { get; set; } // ID of the user who sent the message (either UserId or CoachId)

    [Required]
    [StringLength(20)]
    public required string MessageType { get; set; } = "text"; // "text" or "voice"

    [Required]
    public required string Content { get; set; } // Text content or voice note URL/path

    [StringLength(512)]
    public string? VoiceNoteUrl { get; set; } // URL to voice note audio file (if MessageType is "voice")

    [StringLength(50)]
    public string? VoiceNoteFileName { get; set; } // Original filename of voice note

    [Required]
    [StringLength(50)]
    public required string Status { get; set; } = "sent"; // "sent", "delivered", "read"

    public DateTime? DeliveredAt { get; set; }

    public DateTime? ReadAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [ForeignKey(nameof(CoachId))]
    public User? Coach { get; set; }

    [ForeignKey(nameof(SenderId))]
    public User? Sender { get; set; }

    [ForeignKey(nameof(ConnectionId))]
    public Connection? Connection { get; set; }
}

