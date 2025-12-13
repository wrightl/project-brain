namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;
using ProjectBrain.Domain.Mappers;

public class UserManagementService : IUserManagementService
{
    private readonly AppDbContext _context;

    public UserManagementService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<BaseUserDto>> GetAll()
    {
        var users = await _context.Users
            .Include(u => u.UserRoles)
            .ToListAsync();

        return users.Select(u => u.ToBaseUserDto()).ToList();
    }

    public async Task<BaseUserDto> UpdateRoles(string userId, List<string> roles)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync();

        if (user == null)
        {
            throw new Exception($"User with ID {userId} not found.");
        }

        // Remove existing roles
        _context.UserRoles.RemoveRange(user.UserRoles);

        // Add new roles
        user.UserRoles = roles.Select(roleName => new UserRole
        {
            UserId = userId,
            RoleName = roleName,
            Role = _context.Roles.FirstOrDefault(r => r.Name == roleName)
                   ?? new Role { Name = roleName }
        }).ToList();

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return user.ToBaseUserDto();
    }
}

public interface IUserManagementService
{
    Task<List<BaseUserDto>> GetAll();
    Task<BaseUserDto> UpdateRoles(string userId, List<string> roles);
}

