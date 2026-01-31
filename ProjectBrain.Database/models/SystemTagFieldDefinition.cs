using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class SystemTagFieldDefinition
{
    public Guid Id { get; set; }

    [Required]
    public Guid SystemTagId { get; set; }

    [ForeignKey(nameof(SystemTagId))]
    public SystemTag? SystemTag { get; set; }

    [Required]
    [StringLength(100)]
    public string FieldKey { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Supported UI types: text, textarea, number, rating, select, time.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string InputType { get; set; } = "text";

    public bool Required { get; set; } = false;

    public int FieldOrder { get; set; } = 0;

    [StringLength(200)]
    public string? Placeholder { get; set; }

    [StringLength(500)]
    public string? Hint { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? OptionsJson { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? MinValue { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? MaxValue { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? StepValue { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

