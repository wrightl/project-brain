namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ConversationService(IConversationRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Conversation> Add(Conversation conversation)
    {
        _repository.Add(conversation);
        await _unitOfWork.SaveChangesAsync();
        return conversation;
    }

    public async Task<Conversation?> GetById(Guid id, string userId)
    {
        return await _repository.GetByIdForUserAsync(id, userId);
    }

    public async Task<Conversation?> GetByIdWithMessages(Guid id, string userId)
    {
        return await _repository.GetByIdWithMessagesForUserAsync(id, userId);
    }

    public async Task<IEnumerable<Conversation>> GetAllForUser(string userId)
    {
        return await _repository.GetAllForUserAsync(userId);
    }

    public async Task<Conversation> Update(Conversation conversation)
    {
        _repository.Update(conversation);
        await _unitOfWork.SaveChangesAsync();
        return conversation;
    }

    public async Task<Conversation> Remove(Conversation conversation)
    {
        _repository.Remove(conversation);
        await _unitOfWork.SaveChangesAsync();
        return conversation;
    }
}

public interface IConversationService
{
    Task<Conversation> Add(Conversation conversation);
    Task<Conversation?> GetById(Guid id, string userId);
    Task<Conversation?> GetByIdWithMessages(Guid id, string userId);
    Task<IEnumerable<Conversation>> GetAllForUser(string userId);
    Task<Conversation> Update(Conversation conversation);
    Task<Conversation> Remove(Conversation conversation);
}