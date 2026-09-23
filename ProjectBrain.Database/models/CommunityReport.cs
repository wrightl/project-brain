using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectBrain.Database.Models;

public class CommunityReport
{
    public const string StatusOpen = "open";
    public const string StatusResolved = "resolved";

    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid PostId { get; set; }

    [ForeignKey(nameof(PostId))]
    public CommunityPost? Post { get; set; }

    [Required]
    [StringLength(128)]
    public string ReporterUserId { get; set; } = string.Empty;

    [ForeignKey(nameof(ReporterUserId))]
    public User? Reporter { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Status { get; set; } = StatusOpen;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
