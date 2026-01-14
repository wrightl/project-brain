using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("DeviceTokens")]
public class DeviceToken
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(128)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Token { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Platform { get; set; } // "ios" or "android"

    [StringLength(200)]
    public string? DeviceId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public DateTime? LastValidatedAt { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? InvalidReason { get; set; }

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}

