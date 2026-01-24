using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ApplicationSetting
{
    [Key]
    [StringLength(255)]
    public required string Key { get; set; }

    [Required]
    [StringLength(1000)]
    public required string Value { get; set; }

    [StringLength(100)]
    public string? Category { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(128)]
    public required string UpdatedBy { get; set; } // Admin user ID

    // Navigation property
    [ForeignKey(nameof(UpdatedBy))]
    public User? UpdatedByUser { get; set; }
}
