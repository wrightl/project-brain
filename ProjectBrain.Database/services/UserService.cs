using Microsoft.EntityFrameworkCore;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User> Create(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User?> GetByEmail(string email)
    {
        return await _context.Users.FindAsync(email);
    }

    public async Task<User?> GetById(string Id)
    {
        return await _context.Users.FindAsync(Id);
    }
}

public interface IUserService
{
    Task<User> Create(User user);

    Task<User?> GetById(string Id);

    Task<User?> GetByEmail(string email);
}