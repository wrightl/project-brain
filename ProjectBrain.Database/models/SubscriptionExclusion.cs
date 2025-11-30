using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class SubscriptionExclusion
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(128)]
    public required string UserId { get; set; }

    [Required]
    [StringLength(20)]
    public required string UserType { get; set; } // "user", "coach"

    [Required]
    [StringLength(128)]
    public required string ExcludedBy { get; set; } // Admin user ID

    [Required]
    public DateTime ExcludedAt { get; set; } = DateTime.UtcNow;

    [StringLength(1000)]
    public string? Notes { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [ForeignKey(nameof(ExcludedBy))]
    public User? ExcludedByUser { get; set; }
}

