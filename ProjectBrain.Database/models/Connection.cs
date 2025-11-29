using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Connection
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
    [StringLength(20)]
    public required string Status { get; set; } = "pending"; // "pending", "accepted", "cancelled", "rejected"

    [Required]
    [StringLength(10)]
    public required string RequestedBy { get; set; } // "user" or "coach"

    [StringLength(1000)]
    public string? Message { get; set; }

    [Required]
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RespondedAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [ForeignKey(nameof(CoachId))]
    public User? Coach { get; set; }
}

