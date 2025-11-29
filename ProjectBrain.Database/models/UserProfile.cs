using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class UserProfile
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(128)]
    public required string UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    public DateOnly? DoB { get; set; }

    // User-specific fields
    [StringLength(20)]
    public string? PreferredPronoun { get; set; }
    // Navigation properties
    public ICollection<NeurodiverseTrait> NeurodiverseTraits { get; set; } = new List<NeurodiverseTrait>();

    public UserPreference? Preference { get; set; }
}

