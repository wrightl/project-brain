
using ProjectBrain.Domain;

namespace ProjectBrain.Domain.Mappers;

public static class DtoToDomainMapper
{
    public static User ToUser(this BaseUserDto userDto)
    {
        var user = new User
        {
            Id = userDto.Id,
            Email = userDto.Email,
            FullName = userDto.FullName,
            IsOnboarded = userDto.IsOnboarded,
            StreetAddress = userDto.StreetAddress,
            AddressLine2 = userDto.AddressLine2,
            City = userDto.City,
            StateProvince = userDto.StateProvince,
            PostalCode = userDto.PostalCode,
            Country = userDto.Country
        };

        return user;
    }
}