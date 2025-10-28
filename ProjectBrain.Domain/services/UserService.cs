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

    public async Task<UserDto> Create(UserDto userDto)
    {
        var user = userDto.ToUser();

        user.UserRoles = userDto.Roles.Select(roleName => new UserRole
        {
            UserId = userDto.Id,
            Role = _context.Roles.FirstOrDefault(r => r.Name == roleName)
                   ?? new Role { Name = roleName }
        }).ToList();

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return userDto;
    }

    public async Task<UserDto> Update(UserDto userDto)
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
        user.DoB = updatedUser.DoB;
        user.FavoriteColour = updatedUser.FavoriteColour;
        user.IsOnboarded = updatedUser.IsOnboarded;
        user.PreferredPronoun = updatedUser.PreferredPronoun;
        user.NeurodivergentDetails = updatedUser.NeurodivergentDetails;
        user.Address = updatedUser.Address;
        user.Experience = updatedUser.Experience;

        user.UserRoles = userDto.Roles.Select(roleName => new UserRole
        {
            UserId = userDto.Id,
            Role = _context.Roles.FirstOrDefault(r => r.Name == roleName)
                   ?? new Role { Name = roleName }
        }).ToList();

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return userDto;
    }

    public async Task<UserDto?> GetByEmail(string email)
    {
        var userWithRoles = await _context.Users
            .Include(c => c.UserRoles)
            .Where(u => u.Email == email)
            .FirstOrDefaultAsync();

        Console.WriteLine($"Fetched user: {userWithRoles?.Email}, Roles count: {userWithRoles?.UserRoles.Count}");

        return userWithRoles?.ToUserDto();
    }

    public async Task<UserDto?> GetById(string Id)
    {
        var user = await _context.Users
            .Include(c => c.UserRoles)
            .Where(u => u.Id == Id)
            .FirstOrDefaultAsync();
        return user?.ToUserDto();
    }

    public async Task<UserDto> DeleteById(string Id)
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
        return user?.ToUserDto();
    }
}

public interface IUserService
{
    Task<UserDto> Create(UserDto userDto);
    Task<UserDto> Update(UserDto userDto);

    Task<UserDto?> GetById(string Id);

    Task<UserDto?> GetByEmail(string email);
    Task<UserDto?> DeleteById(string Id);
}

