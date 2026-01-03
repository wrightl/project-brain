using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class OnboardingData
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(128)]
    public required string UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public required string OnboardingJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

