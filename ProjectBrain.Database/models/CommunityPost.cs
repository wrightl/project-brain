using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectBrain.Database.Models;

public class CommunityPost
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ChannelId { get; set; }

    [ForeignKey(nameof(ChannelId))]
    public CommunityChannel? Channel { get; set; }

    [Required]
    [StringLength(128)]
    public string AuthorUserId { get; set; } = string.Empty;

    [ForeignKey(nameof(AuthorUserId))]
    public User? Author { get; set; }

    [Required]
    [StringLength(2000)]
    [Column(TypeName = "nvarchar(2000)")]
    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAt { get; set; }

    public bool IsHidden { get; set; }

    [StringLength(128)]
    public string? HiddenByUserId { get; set; }

    public DateTime? HiddenAt { get; set; }

    public ICollection<CommunityReaction> Reactions { get; set; } = new List<CommunityReaction>();

    public ICollection<CommunityReport> Reports { get; set; } = new List<CommunityReport>();
}
