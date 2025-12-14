using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class JournalEntry
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(128)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string Content { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Summary { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property for many-to-many relationship with Tags
    public ICollection<JournalEntryTag> JournalEntryTags { get; set; } = new List<JournalEntryTag>();
}

