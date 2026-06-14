using System.ComponentModel.DataAnnotations;

public class Country
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [Required]
    [StringLength(2)]
    public required string Code { get; set; }

    public bool IsActive { get; set; } = true;
}
