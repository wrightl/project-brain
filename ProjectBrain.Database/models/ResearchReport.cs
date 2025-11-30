using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ResearchReport
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(128)]
    public required string UserId { get; set; }

    [Required]
    [StringLength(255)]
    public required string Title { get; set; }

    public string? Content { get; set; } // JSON or text content

    [Required]
    [StringLength(50)]
    public required string Status { get; set; } = "pending"; // "pending", "completed", "failed"

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}

