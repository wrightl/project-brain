using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class JournalEntryTag
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid JournalEntryId { get; set; }

    [ForeignKey(nameof(JournalEntryId))]
    public JournalEntry? JournalEntry { get; set; }

    [Required]
    public Guid TagId { get; set; }

    [ForeignKey(nameof(TagId))]
    public Tag? Tag { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

