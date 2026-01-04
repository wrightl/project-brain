using System.Text.Json;
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

    // Auth0 account information
    public string? Connection { get; set; } // e.g., "Username-Password-Authentication", "google-oauth2", "windowslive", etc.
    public bool EmailVerified { get; set; }

    public static BaseUserDto FromJson(string jsonStringUser)
    {
        // Convert from auth0 json string to BaseUserDto
        var auth0User = JsonSerializer.Deserialize<Auth0UserDto>(jsonStringUser, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }) ?? new Auth0UserDto();

        return new BaseUserDto
        {
            Id = auth0User.Id,
            Email = auth0User.Email,
            FullName = auth0User.FullName,
            EmailVerified = auth0User.EmailVerified,
            Connection = auth0User.Identities.FirstOrDefault()?.Connection
        };
    }

    public static string ToJson(BaseUserDto user)
    {
        return JsonSerializer.Serialize(user, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
    }

    public static bool Equals(BaseUserDto user1, BaseUserDto user2)
    {
        return user1.Id == user2.Id && user1.Email == user2.Email && user1.FullName == user2.FullName;
    }

    public static int GetHashCode(BaseUserDto user)
    {
        return user.Id.GetHashCode() ^ user.Email.GetHashCode() ^ user.FullName.GetHashCode() ^ user.EmailVerified.GetHashCode() ^ user.Connection.GetHashCode();
    }
}

public record Auth0UserDto
{
    [JsonPropertyName("user_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWriting)]
    public string Id { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("name")]
    public string FullName { get; set; }

    [JsonPropertyName("email_verified")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWriting)]
    public bool EmailVerified { get; set; }

    [JsonPropertyName("nickname")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWriting)]
    public string Nickname { get; set; }

    [JsonPropertyName("phone_number")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWriting)]
    public string PhoneNumber { get; set; }

    [JsonPropertyName("phone_verified")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWriting)]
    public bool PhoneVerified { get; set; }

    [JsonPropertyName("given_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWriting)]
    public string GivenName { get; set; }

    [JsonPropertyName("family_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWriting)]
    public string FamilyName { get; set; }

    [JsonPropertyName("identities")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWriting)]
    public List<Auth0IdentityDto> Identities { get; set; } = new List<Auth0IdentityDto>();
}

public record Auth0IdentityDto
{
    [JsonPropertyName("connection")]
    public string Connection { get; set; }
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
    public double? AverageRating { get; set; }
    public int RatingCount { get; set; }
}