using System.ComponentModel.DataAnnotations;

public class Tag
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(128)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property for many-to-many relationship with JournalEntries
    public ICollection<JournalEntryTag> JournalEntryTags { get; set; } = new List<JournalEntryTag>();
}

