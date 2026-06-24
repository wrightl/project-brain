using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class UserEpisode
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(128)]
    public required string UserId { get; set; }

    [Required]
    [StringLength(1000)]
    public required string Summary { get; set; }

    [Required]
    [StringLength(100)]
    public string Topic { get; set; } = "general";

    [Required]
    [StringLength(20)]
    public string Outcome { get; set; } = "unknown";

    public Guid? RelatedStrategyId { get; set; }

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

    [ForeignKey(nameof(RelatedStrategyId))]
    public UserCopingStrategy? RelatedStrategy { get; set; }
}
