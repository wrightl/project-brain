namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;
using ProjectBrain.Domain.Mappers;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _repository;
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public UserManagementService(IUserRepository repository, AppDbContext context, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<BaseUserDto>> GetAll()
    {
        var users = await _repository.GetAllAsync();
        // Need to include roles, so we'll use a custom query for now
        var usersWithRoles = await _context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ToListAsync();

        return usersWithRoles.Select(u => u.ToBaseUserDto()).ToList();
    }

    public async Task<(IEnumerable<BaseUserDto> Users, int TotalCount)> GetPaged(int skip, int take)
    {
        var users = await _repository.GetPagedWithRolesAsync(skip, take);
        var totalCount = await _repository.CountAllAsync();
        var userDtos = users.Select(u => u.ToBaseUserDto());
        return (userDtos, totalCount);
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

        _repository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return user.ToBaseUserDto();
    }
}

public interface IUserManagementService
{
    Task<List<BaseUserDto>> GetAll();
    Task<(IEnumerable<BaseUserDto> Users, int TotalCount)> GetPaged(int skip, int take);
    Task<BaseUserDto> UpdateRoles(string userId, List<string> roles);
}

