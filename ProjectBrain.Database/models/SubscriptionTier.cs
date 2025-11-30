using System.ComponentModel.DataAnnotations;

public class SubscriptionTier
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public required string Name { get; set; } // "Free", "Pro", "Ultimate"

    [Required]
    [StringLength(20)]
    public required string UserType { get; set; } // "user", "coach"

    // Features stored as JSON string - can be parsed when needed
    public string? Features { get; set; }
}

