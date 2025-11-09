
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ProjectBrain.Domain.Mappers;

public static class DomainToDtoMapper
{
    public static UserDto ToUserDto(this User user)
    {
        Console.WriteLine("Role count: " + user.UserRoles?.Count);
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            DoB = user.DoB,
            FavoriteColour = user.FavoriteColour,
            IsOnboarded = user.IsOnboarded,
            PreferredPronoun = user.PreferredPronoun,
            Experience = user.Experience,
            NeurodivergentDetails = user.NeurodivergentDetails,
            Address = user.Address,
            Roles = user.UserRoles?.Select(ur => ur.RoleName)?.ToList()
        };
    }
}