using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class SystemTag
{
    public Guid Id { get; set; }

    /// <summary>
    /// Stable identifier used in seed + code (e.g. "sleep", "gratitude").
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SystemTagFieldDefinition> FieldDefinitions { get; set; } = new List<SystemTagFieldDefinition>();
    public ICollection<JournalEntrySystemTag> JournalEntrySystemTags { get; set; } = new List<JournalEntrySystemTag>();
}

