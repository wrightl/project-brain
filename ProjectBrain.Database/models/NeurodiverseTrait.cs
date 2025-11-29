using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class NeurodiverseTrait
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserProfileId { get; set; }

    [ForeignKey(nameof(UserProfileId))]
    public UserProfile? UserProfile { get; set; }

    [Required]
    [StringLength(255)]
    public required string Trait { get; set; }
}

