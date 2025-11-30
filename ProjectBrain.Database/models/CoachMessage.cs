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
    public required string Content { get; set; }

    [Required]
    [StringLength(50)]
    public required string Status { get; set; } = "sent"; // "sent", "delivered", "read"

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [ForeignKey(nameof(CoachId))]
    public User? Coach { get; set; }

    [ForeignKey(nameof(ConnectionId))]
    public Connection? Connection { get; set; }
}

