namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public class ResourceService : IResourceService
{
    private readonly IResourceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ResourceService(IResourceRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Resource> Add(Resource resource)
    {
        _repository.Add(resource);
        await _unitOfWork.SaveChangesAsync();
        return resource;
    }

    public async Task<Resource?> GetForUserById(Guid id, string userId)
    {
        return await _repository.GetByIdForUserAsync(id, userId);
    }

    public async Task<Resource?> GetForUserByLocation(string location, string userId)
    {
        return await _repository.GetByLocationForUserAsync(location, userId);
    }

    public async Task<Resource?> GetForUserByFilename(string filename, string userId)
    {
        return await _repository.GetByFilenameForUserAsync(filename, userId);
    }

    public async Task<IEnumerable<Resource>> GetAllForUser(string userId, int? limit = null)
    {
        var resources = await _repository.GetAllForUserAsync(userId);

        if (limit.HasValue && limit.Value > 0)
        {
            return resources.Take(limit.Value);
        }

        return resources;
    }

    public async Task<Resource?> GetSharedById(Guid id)
    {
        return await _repository.GetSharedByIdAsync(id);
    }

    public async Task<Resource?> GetSharedByLocation(string location)
    {
        return await _repository.GetSharedByLocationAsync(location);
    }

    public async Task<Resource?> GetSharedByFilename(string filename)
    {
        return await _repository.GetSharedByFilenameAsync(filename);
    }

    public async Task<IEnumerable<Resource>> GetAllShared()
    {
        return await _repository.GetAllSharedAsync();
    }

    public async Task<Resource> Update(Resource resource)
    {
        _repository.Update(resource);
        await _unitOfWork.SaveChangesAsync();
        return resource;
    }

    public async Task<Resource> Remove(Resource resource)
    {
        _repository.Remove(resource);
        await _unitOfWork.SaveChangesAsync();
        return resource;
    }
}

public interface IResourceService
{
    Task<Resource> Add(Resource resource);
    Task<Resource?> GetForUserById(Guid id, string userId);
    Task<Resource?> GetForUserByLocation(string location, string userId);
    Task<Resource?> GetForUserByFilename(string filename, string userId);
    Task<IEnumerable<Resource>> GetAllForUser(string userId, int? limit = null);
    Task<Resource?> GetSharedById(Guid id);
    Task<Resource?> GetSharedByFilename(string filename);
    Task<Resource?> GetSharedByLocation(string location);
    Task<IEnumerable<Resource>> GetAllShared();
    Task<Resource> Update(Resource resource);
    Task<Resource> Remove(Resource resource);
}