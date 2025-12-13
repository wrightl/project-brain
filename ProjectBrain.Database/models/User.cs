using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjectBrain.Database.Models;

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

    public bool IsOnboarded { get; set; } = false;

    public DateTime? LastActivityAt { get; set; }

    // Address fields (applicable for any country)
    [StringLength(255)]
    public string? StreetAddress { get; set; }

    [StringLength(255)]
    public string? AddressLine2 { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? StateProvince { get; set; }

    [StringLength(20)]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    // Navigation property for roles
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}