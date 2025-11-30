using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class FileStorageUsage
{
    [Key]
    [StringLength(128)]
    public required string UserId { get; set; }

    [Required]
    public long TotalBytes { get; set; } = 0;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}

