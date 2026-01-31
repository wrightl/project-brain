using System.ComponentModel.DataAnnotations;

public class Achievement
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(100)]
    public required string Key { get; set; }

    [Required]
    [StringLength(150)]
    public required string Title { get; set; }

    [Required]
    [StringLength(1000)]
    public required string Description { get; set; }

    [StringLength(50)]
    public string? IconKey { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

