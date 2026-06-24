using System.ComponentModel.DataAnnotations;

public class MemoryPromotionAudit
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(128)]
    public required string UserId { get; set; }

    public Guid? ConversationId { get; set; }

    [Required]
    [StringLength(20)]
    public required string CandidateType { get; set; }

    [Required]
    [StringLength(1000)]
    public required string CandidateContent { get; set; }

    [Required]
    [StringLength(20)]
    public required string Decision { get; set; }

    [StringLength(200)]
    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
