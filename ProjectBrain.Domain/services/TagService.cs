namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public class TagService : ITagService
{
    private readonly ITagRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public TagService(ITagRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Tag> Add(Tag tag)
    {
        // Check if tag with same name already exists for user
        var existingTag = await _repository.GetByNameForUserAsync(tag.Name, tag.UserId);
        if (existingTag != null)
        {
            return existingTag; // Return existing tag instead of creating duplicate
        }

        _repository.Add(tag);
        await _unitOfWork.SaveChangesAsync();
        return tag;
    }

    public async Task<Tag?> GetById(Guid id, string userId)
    {
        return await _repository.GetByIdForUserAsync(id, userId);
    }

    public async Task<Tag?> GetByName(string name, string userId)
    {
        return await _repository.GetByNameForUserAsync(name, userId);
    }

    public async Task<IEnumerable<Tag>> GetAllForUser(string userId)
    {
        return await _repository.GetAllForUserAsync(userId);
    }

    public async Task<Tag> Update(Tag tag)
    {
        _repository.Update(tag);
        await _unitOfWork.SaveChangesAsync();
        return tag;
    }

    public async Task<Tag> Remove(Tag tag)
    {
        _repository.Remove(tag);
        await _unitOfWork.SaveChangesAsync();
        return tag;
    }

    public async Task<Tag> GetOrCreate(string name, string userId)
    {
        var existingTag = await _repository.GetByNameForUserAsync(name, userId);
        if (existingTag != null)
        {
            return existingTag;
        }

        var newTag = new Tag
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };

        return await Add(newTag);
    }
}

public interface ITagService
{
    Task<Tag> Add(Tag tag);
    Task<Tag?> GetById(Guid id, string userId);
    Task<Tag?> GetByName(string name, string userId);
    Task<IEnumerable<Tag>> GetAllForUser(string userId);
    Task<Tag> Update(Tag tag);
    Task<Tag> Remove(Tag tag);
    Task<Tag> GetOrCreate(string name, string userId);
}

