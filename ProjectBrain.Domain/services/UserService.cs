namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;
using ProjectBrain.Domain.Mappers;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<BaseUserDto> Create(BaseUserDto userDto)
    {
        var user = userDto.ToUser();

        user.UserRoles = userDto.Roles.Select(roleName => new UserRole
        {
            UserId = userDto.Id,
            RoleName = roleName,
            Role = _context.Roles.FirstOrDefault(r => r.Name == roleName)
                   ?? new Role { Name = roleName }
        }).ToList();

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return userDto;
    }

    public async Task<BaseUserDto> Update(BaseUserDto userDto)
    {
        var user = await _context.Users
            .Include(c => c.UserRoles)
            .Where(u => u.Id == userDto.Id)
            .FirstOrDefaultAsync();

        if (user == null)
        {
            throw new Exception($"User with ID {userDto.Id} not found.");
        }

        var updatedUser = userDto.ToUser();

        // Merge updated fields
        user.FullName = updatedUser.FullName;
        user.IsOnboarded = updatedUser.IsOnboarded;
        user.StreetAddress = updatedUser.StreetAddress;
        user.AddressLine2 = updatedUser.AddressLine2;
        user.City = updatedUser.City;
        user.StateProvince = updatedUser.StateProvince;
        user.PostalCode = updatedUser.PostalCode;
        user.Country = updatedUser.Country;

        user.UserRoles = userDto.Roles.Select(roleName => new UserRole
        {
            UserId = userDto.Id,
            RoleName = roleName,
            Role = _context.Roles.FirstOrDefault(r => r.Name == roleName)
                   ?? new Role { Name = roleName }
        }).ToList();

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return userDto;
    }

    public async Task<BaseUserDto?> GetByEmail(string email)
    {
        var userWithRoles = await _context.Users
            .Include(c => c.UserRoles)
            .Where(u => u.Email == email)
            .FirstOrDefaultAsync();

        Console.WriteLine($"Fetched user: {userWithRoles?.Email}, Roles count: {userWithRoles?.UserRoles.Count}");

        return userWithRoles?.ToBaseUserDto();
    }

    public async Task<BaseUserDto?> GetById(string Id)
    {
        var user = await _context.Users
            .Include(c => c.UserRoles)
            .Where(u => u.Id == Id)
            .FirstOrDefaultAsync();
        return user?.ToBaseUserDto();
    }

    public async Task<BaseUserDto> DeleteById(string Id)
    {
        var user = await _context.Users
            .Where(u => u.Id == Id)
            .FirstOrDefaultAsync();
        if (user == null)
        {
            throw new Exception($"User with ID {Id} not found.");
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return user.ToBaseUserDto();
    }
}

public interface IUserService
{
    Task<BaseUserDto> Create(BaseUserDto userDto);
    Task<BaseUserDto> Update(BaseUserDto userDto);

    Task<BaseUserDto?> GetById(string Id);

    Task<BaseUserDto?> GetByEmail(string email);
    Task<BaseUserDto> DeleteById(string Id);
}

