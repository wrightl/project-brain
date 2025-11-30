using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class SubscriptionSettings
{
    [Key]
    public int Id { get; set; } = 1; // Singleton - only one record

    [Required]
    public bool EnableUserSubscriptions { get; set; } = true;

    [Required]
    public bool EnableCoachSubscriptions { get; set; } = true;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(128)]
    public required string UpdatedBy { get; set; } // Admin user ID

    // Navigation property
    [ForeignKey(nameof(UpdatedBy))]
    public User? UpdatedByUser { get; set; }
}

