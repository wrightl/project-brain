using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class UserPreference
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserProfileId { get; set; }

    [ForeignKey(nameof(UserProfileId))]
    public UserProfile? UserProfile { get; set; }

    [StringLength(1000)]
    public string? Preferences { get; set; }
}

