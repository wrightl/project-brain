
using ProjectBrain.Domain;

namespace ProjectBrain.Domain.Mappers;

public static class DtoToDomainMapper
{
    public static User ToUser(this UserDto userDto)
    {
        var user = new User
        {
            Id = userDto.Id,
            Email = userDto.Email,
            FullName = userDto.FullName,
            DoB = userDto.DoB,
            FavoriteColour = userDto.FavoriteColour,
            IsOnboarded = userDto.IsOnboarded,
            PreferredPronoun = userDto.PreferredPronoun,
            NeurodivergentDetails = userDto.NeurodivergentDetails,
            Address = userDto.Address,
            Experience = userDto.Experience
        };

        return user;
    }
}