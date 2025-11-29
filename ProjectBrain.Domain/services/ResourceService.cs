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

    public async Task<Resource?> GetForUserById(Guid id, string userId)
    {
        return await _context.Resources
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && c.IsShared == false);
    }

    public async Task<Resource?> GetForUserByLocation(string location, string userId)
    {
        return await _context.Resources
            .FirstOrDefaultAsync(c => c.Location == location && c.UserId == userId && c.IsShared == false);
    }

    public async Task<Resource?> GetForUserByFilename(string filename, string userId)
    {
        return await _context.Resources
            .FirstOrDefaultAsync(c => c.FileName == filename && c.UserId == userId && c.IsShared == false);
    }

    public async Task<IEnumerable<Resource>> GetAllForUser(string userId)
    {
        return await _context.Resources
            .Where(c => c.UserId == userId && c.IsShared == false)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Resource?> GetSharedById(Guid id)
    {
        return await _context.Resources
            .FirstOrDefaultAsync(c => c.Id == id && c.IsShared && (c.UserId == null || c.UserId == string.Empty));
    }

    public async Task<Resource?> GetSharedByLocation(string location)
    {
        return await _context.Resources
            .FirstOrDefaultAsync(c => c.Location == location && c.IsShared && (c.UserId == null || c.UserId == string.Empty));
    }

    public async Task<Resource?> GetSharedByFilename(string filename)
    {
        return await _context.Resources
            .FirstOrDefaultAsync(c => c.FileName == filename && c.IsShared && (c.UserId == null || c.UserId == string.Empty));
    }

    public async Task<IEnumerable<Resource>> GetAllShared()
    {
        return await _context.Resources
            .Where(c => c.IsShared && (c.UserId == null || c.UserId == string.Empty))
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
    Task<Resource?> GetForUserById(Guid id, string userId);
    Task<Resource?> GetForUserByLocation(string location, string userId);
    Task<Resource?> GetForUserByFilename(string filename, string userId);
    Task<IEnumerable<Resource>> GetAllForUser(string userId);
    Task<Resource?> GetSharedById(Guid id);
    Task<Resource?> GetSharedByFilename(string filename);
    Task<Resource?> GetSharedByLocation(string location);
    Task<IEnumerable<Resource>> GetAllShared();
    Task<Resource> Update(Resource resource);
    Task<Resource> Remove(Resource resource);
}