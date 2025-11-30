using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class UserSubscription
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
    public int TierId { get; set; }

    [StringLength(255)]
    public string? StripeCustomerId { get; set; }

    [StringLength(255)]
    public string? StripeSubscriptionId { get; set; }

    [StringLength(255)]
    public string? StripePriceId { get; set; }

    [Required]
    [StringLength(50)]
    public required string Status { get; set; } // "active", "trialing", "past_due", "canceled", "incomplete", "expired"

    public DateTime? TrialEndsAt { get; set; }

    [Required]
    public DateTime CurrentPeriodStart { get; set; }

    [Required]
    public DateTime CurrentPeriodEnd { get; set; }

    public DateTime? CanceledAt { get; set; }

    public DateTime? ExpiredAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [ForeignKey(nameof(TierId))]
    public SubscriptionTier? Tier { get; set; }
}

