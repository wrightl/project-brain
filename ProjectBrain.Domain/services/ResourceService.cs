namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;

public class ResourceService : IResourceService
{
    private readonly AppDbContext _context;

    public ResourceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Resource> Add(Resource resource)
    {
        _context.Resources.Add(resource);
        await _context.SaveChangesAsync();
        return resource;
    }

    public async Task<Resource?> GetById(Guid id, string userId)
    {
        return await _context.Resources
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
    }

    public async Task<IEnumerable<Resource>> GetAllForUser(string userId)
    {
        return await _context.Resources
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Resource> Update(Resource resource)
    {
        _context.Resources.Update(resource);
        await _context.SaveChangesAsync();
        return resource;
    }

    public async Task<Resource> Remove(Resource resource)
    {
        _context.Resources.Remove(resource);
        await _context.SaveChangesAsync();
        return resource;
    }
}

public interface IResourceService
{
    Task<Resource> Add(Resource resource);
    Task<Resource?> GetById(Guid id, string userId);
    Task<IEnumerable<Resource>> GetAllForUser(string userId);
    Task<Resource> Update(Resource resource);
    Task<Resource> Remove(Resource resource);
}