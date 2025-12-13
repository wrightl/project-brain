using System.Text.Json.Serialization;
using ProjectBrain.Database.Models;

namespace ProjectBrain.Domain;

public record BaseUserDto
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string? FirstName => FullName.Split(' ').FirstOrDefault();
    public List<string> Roles { get; set; } = new List<string>();
    public bool IsOnboarded { get; set; }
    public DateTime? LastActivityAt { get; set; }

    // Address fields
    public string? StreetAddress { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
}

public record UserDto : BaseUserDto
{
    public string? UserProfileId { get; set; }
    public DateOnly? DoB { get; set; }
    public string? PreferredPronoun { get; set; }
    public List<string> NeurodiverseTraits { get; set; } = new List<string>();
    public string? Preferences { get; set; }
}

public record CoachDto : BaseUserDto
{
    public string? CoachProfileId { get; set; }
    public List<string> Qualifications { get; set; } = new List<string>();
    public List<string> Specialisms { get; set; } = new List<string>();
    public List<string> AgeGroups { get; set; } = new List<string>();
    public AvailabilityStatus? AvailabilityStatus { get; set; }
}