using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ExternalIntegration
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(128)]
    public required string UserId { get; set; }

    [Required]
    [StringLength(50)]
    public required string IntegrationType { get; set; } // "google_calendar", "notion", etc.

    [Required]
    [StringLength(50)]
    public required string Status { get; set; } = "disconnected"; // "connected", "disconnected", "error"

    // Encrypted tokens - should be encrypted at application level
    [StringLength(2000)]
    public string? AccessToken { get; set; }

    [StringLength(2000)]
    public string? RefreshToken { get; set; }

    public DateTime? ExpiresAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}

