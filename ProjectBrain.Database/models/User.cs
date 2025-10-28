using System.ComponentModel.DataAnnotations;

public class User
{
    [Key]
    [StringLength(128)]
    public required string Id { get; init; }

    [StringLength(255)]
    public required string Email { get; init; }

    [StringLength(255)]
    public required string FullName { get; set; }
    public string? FirstName => FullName.Split(' ').FirstOrDefault();
    [StringLength(100)]
    public required string FavoriteColour { get; set; }

    public required DateOnly DoB { get; set; }

    public bool IsOnboarded { get; set; } = false;

    // User-specific fields
    [StringLength(20)]
    public string? PreferredPronoun { get; set; }
    public string? NeurodivergentDetails { get; set; }

    // Coach-specific fields
    [StringLength(364)]
    public string? Address { get; set; }
    public string? Experience { get; set; }

    // Navigation property for roles
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}