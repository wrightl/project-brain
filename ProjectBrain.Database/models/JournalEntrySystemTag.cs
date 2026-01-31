using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class JournalEntrySystemTag
{
    public Guid Id { get; set; }

    [Required]
    public Guid JournalEntryId { get; set; }

    [ForeignKey(nameof(JournalEntryId))]
    public JournalEntry? JournalEntry { get; set; }

    [Required]
    public Guid SystemTagId { get; set; }

    [ForeignKey(nameof(SystemTagId))]
    public SystemTag? SystemTag { get; set; }

    /// <summary>
    /// JSON object mapping fieldKey -> value (string/number/bool).
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? ResponsesJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

