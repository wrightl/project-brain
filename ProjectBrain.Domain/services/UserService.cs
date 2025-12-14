namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;
using ProjectBrain.Domain.Mappers;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUserRepository repository, AppDbContext context, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _context = context;
        _unitOfWork = unitOfWork;
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

        _repository.Add(user);
        await _unitOfWork.SaveChangesAsync();
        return userDto;
    }

    public async Task<BaseUserDto> Update(BaseUserDto userDto)
    {
        // Get tracked entity for update (not using AsNoTracking)
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

        _repository.Update(user);
        await _unitOfWork.SaveChangesAsync();
        return userDto;
    }

    public async Task<BaseUserDto?> GetByEmail(string email)
    {
        var user = await _repository.GetByEmailWithRolesAsync(email);
        Console.WriteLine($"Fetched user: {user?.Email}, Roles count: {user?.UserRoles.Count}");
        return user?.ToBaseUserDto();
    }

    public async Task<BaseUserDto?> GetById(string Id)
    {
        var user = await _repository.GetByIdWithRolesAsync(Id);
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

        _repository.Remove(user);
        await _unitOfWork.SaveChangesAsync();
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

