using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class UserCopingStrategy
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(128)]
    public required string UserId { get; set; }

    [Required]
    [StringLength(100)]
    public required string Title { get; set; }

    [Required]
    [StringLength(1000)]
    public required string Description { get; set; }

    [StringLength(50)]
    public string? IconKey { get; set; }

    [Range(1, 5)]
    public int? Rating { get; set; }

    [Required]
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}

