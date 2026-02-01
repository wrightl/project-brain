using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ReferralInvite
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(128)]
    public required string InviterUserId { get; set; }

    [Required]
    [StringLength(255)]
    public required string RecipientEmail { get; set; }

    [Required]
    [StringLength(255)]
    public required string RecipientEmailNormalized { get; set; }

    /// <summary>
    /// SHA-256 hash (hex) of the raw invite token.
    /// </summary>
    [Required]
    [StringLength(64)]
    public required string TokenHash { get; set; }

    [Required]
    [StringLength(20)]
    public required string Status { get; set; } // "Pending", "Accepted", "Rewarded", "Expired"

    [Required]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastSentAt { get; set; }

    [Required]
    public int ResendCount { get; set; } = 0;

    [Required]
    public DateTime ExpiresAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    [StringLength(128)]
    public string? AcceptedByUserId { get; set; }

    public DateTime? RewardedAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(InviterUserId))]
    public User? Inviter { get; set; }

    [ForeignKey(nameof(AcceptedByUserId))]
    public User? AcceptedByUser { get; set; }
}

