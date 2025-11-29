using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class CoachAgeGroup
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CoachProfileId { get; set; }

    [ForeignKey(nameof(CoachProfileId))]
    public CoachProfile? CoachProfile { get; set; }

    [Required]
    [StringLength(100)]
    public required string AgeGroup { get; set; }
}

