using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class CoachQualification
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CoachProfileId { get; set; }

    [ForeignKey(nameof(CoachProfileId))]
    public CoachProfile? CoachProfile { get; set; }

    [Required]
    [StringLength(255)]
    public required string Qualification { get; set; }
}

