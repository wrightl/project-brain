using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjectBrain.Database.Models;

public class CoachProfile
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(128)]
    public required string UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public AvailabilityStatus? AvailabilityStatus { get; set; }

    // Navigation properties for one-to-many relationships
    public ICollection<CoachQualification> Qualifications { get; set; } = new List<CoachQualification>();
    public ICollection<CoachSpecialism> Specialisms { get; set; } = new List<CoachSpecialism>();
    public ICollection<CoachAgeGroup> AgeGroups { get; set; } = new List<CoachAgeGroup>();
}

