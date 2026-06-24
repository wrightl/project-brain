using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class UserFact
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(128)]
    public required string UserId { get; set; }

    [Required]
    [StringLength(500)]
    public required string Content { get; set; }

    [Required]
    [StringLength(50)]
    public string Category { get; set; } = "general";

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = MemoryStatuses.Provisional;

    public double Confidence { get; set; }

    [Required]
    [StringLength(64)]
    public required string ContentHash { get; set; }

    public Guid? SourceConversationId { get; set; }

    public int ObservationCount { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastRetrievedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? PinnedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}

public static class MemoryStatuses
{
    public const string Provisional = "provisional";
    public const string Active = "active";
    public const string Superseded = "superseded";
    public const string Rejected = "rejected";
}
