namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;

public class ConversationService : IConversationService
{
    private readonly AppDbContext _context;

    public ConversationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation> Add(Conversation conversation)
    {
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();
        return conversation;
    }

    public async Task<Conversation?> GetById(Guid id, string userId)
    {
        return await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
    }

    public async Task<Conversation?> GetByIdWithMessages(Guid id, string userId)
    {
        return await _context.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
    }

    public async Task<IEnumerable<Conversation>> GetAllForUser(string userId)
    {
        return await _context.Conversations
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Conversation> Update(Conversation conversation)
    {
        _context.Conversations.Update(conversation);
        await _context.SaveChangesAsync();
        return conversation;
    }

    public async Task<Conversation> Remove(Conversation conversation)
    {
        _context.Conversations.Remove(conversation);
        await _context.SaveChangesAsync();
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