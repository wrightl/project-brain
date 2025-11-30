using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class UsageTracking
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(128)]
    public required string UserId { get; set; }

    [Required]
    [StringLength(50)]
    public required string UsageType { get; set; } // "ai_query", "coach_message", "client_message", "file_upload", "research_report"

    [Required]
    [StringLength(20)]
    public required string PeriodType { get; set; } // "daily", "monthly"

    [Required]
    public DateTime PeriodStart { get; set; }

    [Required]
    public int Count { get; set; } = 0;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}

