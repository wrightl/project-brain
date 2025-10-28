using System.ComponentModel.DataAnnotations;

public class Role
{
    [Key]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
    [StringLength(255)]
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation property for users with this role
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}