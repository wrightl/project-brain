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

    // Geo fields (derived from address)
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

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

    /// <summary>
    /// Deserializes Auth0 Management API user response (snake_case) into Auth0UserDto.
    /// </summary>
    public static Auth0UserDto FromJson(string json)
    {
        return JsonSerializer.Deserialize<Auth0UserDto>(json) ?? new Auth0UserDto();
    }
}

public record Auth0IdentityDto
{
    [JsonPropertyName("connection")]
    public string Connection { get; set; }
}

/// <summary>
/// DTO for Auth0 Management API PATCH /api/v2/users/{id}.
/// Only includes root attributes that Auth0 accepts; uses snake_case for request body.
/// </summary>
public record Auth0UserPatchRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// Builds a patch request from the current Auth0 user and applies only the properties
    /// supported by Auth0 PATCH from the given BaseUserDto.
    /// </summary>
    public static Auth0UserPatchRequest FromAuth0UserAndApply(Auth0UserDto auth0User, BaseUserDto updates)
    {
        return new Auth0UserPatchRequest
        {
            Name = updates.FullName ?? auth0User.FullName ?? string.Empty,
            Email = updates.Email ?? auth0User.Email ?? string.Empty,
            Nickname = updates.FullName?.Split(' ').FirstOrDefault() ?? auth0User.Nickname ?? string.Empty
        };
    }

    /// <summary>
    /// Returns true if the patchable fields (name, email, nickname) are equal.
    /// </summary>
    public static bool PatchableFieldsEqual(Auth0UserDto auth0User, BaseUserDto dto)
    {
        var dtoNickname = dto.FullName?.Split(' ').FirstOrDefault() ?? string.Empty;
        var auth0Nickname = auth0User.Nickname ?? string.Empty;
        return string.Equals(auth0User.FullName, dto.FullName, StringComparison.Ordinal)
               && string.Equals(auth0User.Email, dto.Email, StringComparison.Ordinal)
               && string.Equals(auth0Nickname, dtoNickname, StringComparison.Ordinal);
    }

    public static string ToJson(Auth0UserPatchRequest request)
    {
        return JsonSerializer.Serialize(request);
    }
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
    public string? Bio { get; set; }
    public string? ImageUrl { get; set; }
    public double? AverageRating { get; set; }
    public int RatingCount { get; set; }
}