using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ReferralReward
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ReferralInviteId { get; set; }

    [Required]
    [StringLength(128)]
    public required string BeneficiaryUserId { get; set; } // inviter or invitee

    [Required]
    public int Months { get; set; }

    [Required]
    [StringLength(20)]
    public required string Status { get; set; } // "Pending", "Applied"

    public DateTime? AppliedAt { get; set; }

    [StringLength(255)]
    public string? AppliedToStripeSubscriptionId { get; set; }

    [StringLength(255)]
    public string? TriggeringStripeInvoiceId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ReferralInviteId))]
    public ReferralInvite? ReferralInvite { get; set; }

    [ForeignKey(nameof(BeneficiaryUserId))]
    public User? BeneficiaryUser { get; set; }
}

