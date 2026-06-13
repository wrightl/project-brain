using System.ComponentModel.DataAnnotations;

public class CoachSpecialismOption
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public required string Name { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
